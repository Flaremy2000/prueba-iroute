[CmdletBinding()]
param(
    [string]$BaseUrl = "http://localhost:5238",
    [string]$SqlServer = ".\SQLEXPRESS",
    [string]$Database = "IRouteCommerce",
    [switch]$SkipReset
)

$ErrorActionPreference = "Stop"
$script:Passes = 0
$script:Failures = 0
$root = $PSScriptRoot

function Write-Pass {
    param([string]$Message)
    $script:Passes++
    Write-Host "PASS  $Message" -ForegroundColor Green
}

function Write-Fail {
    param([string]$Message, [string]$Detail = "")
    $script:Failures++
    Write-Host "FAIL  $Message" -ForegroundColor Red
    if ($Detail) {
        Write-Host "      $Detail" -ForegroundColor DarkRed
    }
}

function Assert-Equal {
    param($Expected, $Actual, [string]$Message)
    if ($Expected -eq $Actual) {
        Write-Pass $Message
    }
    else {
        Write-Fail $Message "esperado=$Expected actual=$Actual"
    }
}

function Assert-Status {
    param($Response, [int]$Expected, [string]$Message)
    Assert-Equal $Expected $Response.Status $Message
}

function Assert-Json {
    param($Json, [string]$Property, $Expected, [string]$Message)
    $actual = $null
    if ($null -ne $Json) {
        $propertyInfo = $Json.PSObject.Properties[$Property]
        if ($null -ne $propertyInfo) {
            $actual = $propertyInfo.Value
        }
    }
    Assert-Equal $Expected $actual $Message
}

function Assert-Detail {
    param($Response, [string]$Expected, [string]$Message)
    $actual = $null
    if ($null -ne $Response.Json) {
        $actual = $Response.Json.detail
    }
    Assert-Equal $Expected $actual $Message
}

function Invoke-Curl {
    param([string[]]$CurlArgs)
    $output = & curl.exe -s -w "`n%{http_code}" @CurlArgs
    $text = ($output | Out-String).Trim()

    if ([string]::IsNullOrWhiteSpace($text)) {
        return [pscustomobject]@{ Status = 0; Body = ""; Json = $null }
    }

    $lines = $text -split "`r?`n"
    $status = 0
    [void][int]::TryParse($lines[-1], [ref]$status)
    $body = if ($lines.Count -gt 1) {
        ($lines[0..($lines.Count - 2)] -join "`n").Trim()
    }
    else {
        ""
    }

    $json = $null
    if ($body) {
        try {
            $json = $body | ConvertFrom-Json
        }
        catch {
            $json = $null
        }
    }

    return [pscustomobject]@{ Status = $status; Body = $body; Json = $json }
}

function Invoke-Upload {
    param([string]$Path)
    Invoke-Curl -CurlArgs @(
        "-X", "POST", "$BaseUrl/api/commerce/upload",
        "-F", "file=@$Path"
    )
}

function Invoke-Process {
    param([string]$Path)
    Invoke-Curl -CurlArgs @(
        "-X", "POST", "$BaseUrl/api/commerce/process",
        "-H", "Content-Type: application/json",
        "--data-binary", "@$Path"
    )
}

function Invoke-Get {
    param([string]$Query)
    Invoke-Curl -CurlArgs @("$BaseUrl/api/commerce/quarantine?$Query")
}

function Reset-TestData {
    if ($SkipReset) {
        Write-Host "SKIP  limpieza de datos (-SkipReset)" -ForegroundColor Yellow
        return
    }

    $query = @"
SET NOCOUNT ON;
DELETE FROM dbo.commerce WHERE pc_processdate BETWEEN '2026-11-01' AND '2026-11-09';
DELETE FROM dbo.commerce_quarantine WHERE pc_processdate BETWEEN '2026-11-01' AND '2026-11-09';
"@

    $output = & sqlcmd -S $SqlServer -d $Database -E -C -b -Q $query 2>&1
    if ($LASTEXITCODE -ne 0) {
        throw "No se pudieron limpiar los datos de prueba: $($output | Out-String)"
    }

    Write-Host "LIMPIEZA  datos 2026-11-01..2026-11-09" -ForegroundColor Cyan
}

function Assert-ApiAvailable {
    $response = Invoke-Get "page=1&pageSize=1"
    if ($response.Status -ne 200) {
        throw "La API no responde en $BaseUrl. Status=$($response.Status). Body=$($response.Body)"
    }

    Write-Host "API       $BaseUrl" -ForegroundColor Cyan
}

try {
    Reset-TestData
    Assert-ApiAvailable

    # ------------------------------------------------------------------
    # 1. Éxito total
    # ------------------------------------------------------------------
    $r = Invoke-Upload (Join-Path $root "upload\success\commerce_01112026.csv")
    Assert-Status $r 200 "upload success"
    Assert-Json $r.Json "rowsRead" 4 "upload success rowsRead"
    Assert-Json $r.Json "rowsInserted" 4 "upload success rowsInserted"

    $r = Invoke-Process (Join-Path $root "process\process-01112026.json")
    Assert-Status $r 200 "process success"
    Assert-Json $r.Json "quarantined" 0 "process success quarantined"
    Assert-Json $r.Json "remaining" 4 "process success remaining"

    $r = Invoke-Get "processDate=2026-11-01&page=1&pageSize=10"
    Assert-Status $r 200 "get success"
    Assert-Json $r.Json "total" 0 "get success total"

    # ------------------------------------------------------------------
    # 2. Mixto: válidos + inválidos
    # ------------------------------------------------------------------
    $r = Invoke-Upload (Join-Path $root "upload\mixed\commerce_02112026.csv")
    Assert-Status $r 200 "upload mixed"
    Assert-Json $r.Json "rowsRead" 7 "upload mixed rowsRead"
    Assert-Json $r.Json "rowsInserted" 7 "upload mixed rowsInserted"

    $r = Invoke-Process (Join-Path $root "process\process-02112026.json")
    Assert-Status $r 200 "process mixed"
    Assert-Json $r.Json "quarantined" 5 "process mixed quarantined"
    Assert-Json $r.Json "remaining" 2 "process mixed remaining"

    $r = Invoke-Get "processDate=2026-11-02&page=1&pageSize=10"
    Assert-Status $r 200 "get mixed"
    Assert-Json $r.Json "total" 5 "get mixed total"
    Assert-Equal 5 $r.Json.items.Count "get mixed item count"

    $r = Invoke-Get "processDate=2026-11-02&search=Letras&page=1&pageSize=10"
    Assert-Status $r 200 "get mixed search name"
    Assert-Json $r.Json "total" 1 "get mixed search name total"
    Assert-Equal "Comercio Con Letras" $r.Json.items[0].merchantName "get mixed search name item"

    $r = Invoke-Get "processDate=2026-11-02&search=20-002&page=1&pageSize=10"
    Assert-Status $r 200 "get mixed search document"
    Assert-Json $r.Json "total" 1 "get mixed search document total"
    Assert-Equal "Comercio Con Guion" $r.Json.items[0].merchantName "get mixed search document item"

    $r = Invoke-Get "processDate=2026-11-02&page=2&pageSize=2"
    Assert-Status $r 200 "get mixed pagination"
    Assert-Json $r.Json "total" 5 "get mixed pagination total"
    Assert-Equal 2 $r.Json.items.Count "get mixed pagination item count"

    # ------------------------------------------------------------------
    # 3. Todas inválidas
    # ------------------------------------------------------------------
    $r = Invoke-Upload (Join-Path $root "upload\all-invalid\commerce_07112026.csv")
    Assert-Status $r 200 "upload all-invalid"
    Assert-Json $r.Json "rowsRead" 3 "upload all-invalid rowsRead"
    Assert-Json $r.Json "rowsInserted" 3 "upload all-invalid rowsInserted"

    $r = Invoke-Process (Join-Path $root "process\process-07112026.json")
    Assert-Status $r 200 "process all-invalid"
    Assert-Json $r.Json "quarantined" 3 "process all-invalid quarantined"
    Assert-Json $r.Json "remaining" 0 "process all-invalid remaining"

    $r = Invoke-Get "processDate=2026-11-07&page=1&pageSize=10"
    Assert-Status $r 200 "get all-invalid"
    Assert-Json $r.Json "total" 3 "get all-invalid total"

    # ------------------------------------------------------------------
    # 4. Espacios
    # ------------------------------------------------------------------
    $r = Invoke-Upload (Join-Path $root "upload\spaces\commerce_09112026.csv")
    Assert-Status $r 200 "upload spaces"
    Assert-Json $r.Json "rowsInserted" 3 "upload spaces rowsInserted"

    $r = Invoke-Process (Join-Path $root "process\process-09112026.json")
    Assert-Status $r 200 "process spaces"
    Assert-Json $r.Json "quarantined" 2 "process spaces quarantined"
    Assert-Json $r.Json "remaining" 1 "process spaces remaining"

    $r = Invoke-Get "processDate=2026-11-09&page=1&pageSize=10"
    Assert-Status $r 200 "get spaces"
    Assert-Json $r.Json "total" 2 "get spaces total"

    # ------------------------------------------------------------------
    # 5. Delimitador ;
    # ------------------------------------------------------------------
    $r = Invoke-Upload (Join-Path $root "upload\semicolon\commerce_04112026.csv")
    Assert-Status $r 200 "upload semicolon"
    Assert-Json $r.Json "rowsInserted" 2 "upload semicolon rowsInserted"

    $r = Invoke-Process (Join-Path $root "process\process-04112026.json")
    Assert-Status $r 200 "process semicolon"
    Assert-Json $r.Json "quarantined" 0 "process semicolon quarantined"
    Assert-Json $r.Json "remaining" 2 "process semicolon remaining"

    # ------------------------------------------------------------------
    # 6. Fecha con hora
    # ------------------------------------------------------------------
    $r = Invoke-Upload (Join-Path $root "upload\timestamp\commerce_05112026.csv")
    Assert-Status $r 200 "upload timestamp"
    Assert-Json $r.Json "rowsInserted" 2 "upload timestamp rowsInserted"

    $r = Invoke-Process (Join-Path $root "process\process-05112026.json")
    Assert-Status $r 200 "process timestamp"
    Assert-Json $r.Json "quarantined" 0 "process timestamp quarantined"
    Assert-Json $r.Json "remaining" 2 "process timestamp remaining"

    # ------------------------------------------------------------------
    # 7. Idempotencia del proceso
    # ------------------------------------------------------------------
    $r = Invoke-Process (Join-Path $root "process\process-02112026.json")
    Assert-Status $r 200 "process mixed idempotent"
    Assert-Json $r.Json "quarantined" 0 "process mixed idempotent quarantined"
    Assert-Json $r.Json "remaining" 2 "process mixed idempotent remaining"

    $r = Invoke-Process (Join-Path $root "process\process-01112026.json")
    Assert-Status $r 200 "process success idempotent"
    Assert-Json $r.Json "quarantined" 0 "process success idempotent quarantined"
    Assert-Json $r.Json "remaining" 4 "process success idempotent remaining"

    $r = Invoke-Process (Join-Path $root "process\process-07112026.json")
    Assert-Status $r 200 "process all-invalid idempotent"
    Assert-Json $r.Json "quarantined" 0 "process all-invalid idempotent quarantined"
    Assert-Json $r.Json "remaining" 0 "process all-invalid idempotent remaining"

    # ------------------------------------------------------------------
    # 8. Errores de carga
    # ------------------------------------------------------------------
    $uploadErrors = @(
        @{ Name = "empty"; Path = "upload\errors\empty\commerce_03112026.csv"; Detail = "Debes enviar un archivo CSV no vacío." },
        @{ Name = "headers-only"; Path = "upload\errors\headers-only\commerce_03112026.csv"; Detail = "El archivo no contiene registros." },
        @{ Name = "invalid-file-name"; Path = "upload\errors\invalid-file-name\comercio_03112026.csv"; Detail = "El archivo debe llamarse commerce_DDMMYYYY.csv con una fecha válida." },
        @{ Name = "invalid-date-in-name"; Path = "upload\errors\invalid-date-in-name\commerce_32132026.csv"; Detail = "El archivo debe llamarse commerce_DDMMYYYY.csv con una fecha válida." },
        @{ Name = "missing-name-column"; Path = "upload\errors\missing-name-column\commerce_03112026.csv"; Detail = "El archivo no contiene las columnas obligatorias: pc_nomcomred." },
        @{ Name = "missing-doc-column"; Path = "upload\errors\missing-doc-column\commerce_03112026.csv"; Detail = "El archivo no contiene las columnas obligatorias: pc_numdoc." },
        @{ Name = "missing-date-column"; Path = "upload\errors\missing-date-column\commerce_03112026.csv"; Detail = "El archivo no contiene las columnas obligatorias: pc_processdate." },
        @{ Name = "date-mismatch"; Path = "upload\errors\date-mismatch\commerce_03112026.csv"; Detail = "El archivo contiene registros con fecha 2026-11-04, pero el nombre indica 2026-11-03." },
        @{ Name = "invalid-date-cell"; Path = "upload\errors\invalid-date-cell\commerce_03112026.csv"; Detail = "La fila 2 tiene una fecha de proceso inválida: '2026-13-40'." },
        @{ Name = "malformed-csv"; Path = "upload\errors\malformed-csv\commerce_03112026.csv"; Detail = "La fila 2 tiene datos inválidos." },
        @{ Name = "empty-process-date"; Path = "upload\errors\empty-process-date\commerce_06112026.csv"; Detail = "La fila 2 tiene una fecha de proceso inválida: ''." },
        @{ Name = "long-name"; Path = "upload\errors\long-name\commerce_08112026.csv"; Detail = "La fila 2 tiene un nombre de comercio mayor a 200 caracteres." },
        @{ Name = "long-doc"; Path = "upload\errors\long-doc\commerce_08112026.csv"; Detail = "La fila 2 tiene un número de documento mayor a 50 caracteres." },
        @{ Name = "duplicate-headers"; Path = "upload\errors\duplicate-headers\commerce_08112026.csv"; Detail = "El archivo contiene columnas duplicadas: pc_numdoc." }
    )

    foreach ($case in $uploadErrors) {
        $r = Invoke-Upload (Join-Path $root $case.Path)
        Assert-Status $r 400 "upload error: $($case.Name)"
        Assert-Detail $r $case.Detail "upload error detail: $($case.Name)"
    }

    # ------------------------------------------------------------------
    # 8b. Errores HTTP y de transporte
    # ------------------------------------------------------------------
    $r = Invoke-Curl -CurlArgs @("-X", "GET", "$BaseUrl/api/commerce/upload")
    Assert-Status $r 405 "upload method not allowed"

    $r = Invoke-Curl -CurlArgs @(
        "-X", "POST", "$BaseUrl/api/commerce/upload",
        "-H", "Content-Type: application/json",
        "--data-binary", "@$(Join-Path $root 'process\process-empty.json')"
    )
    Assert-Status $r 415 "upload wrong content type"

    $r = Invoke-Curl -CurlArgs @(
        "-X", "POST", "$BaseUrl/api/commerce/upload",
        "-F", "other=value"
    )
    Assert-Status $r 400 "upload missing file part"
    Assert-Detail $r "Debes enviar un archivo CSV no vacío." "upload missing file part detail"

    $largePath = Join-Path ([System.IO.Path]::GetTempPath()) "commerce_99112026.csv"
    $fs = [System.IO.File]::Create($largePath)
    try {
        $fs.SetLength(21MB)
    }
    finally {
        $fs.Dispose()
    }

    try {
        $r = Invoke-Upload $largePath
        Assert-Status $r 400 "upload file too large"
    }
    finally {
        Remove-Item $largePath -Force -ErrorAction SilentlyContinue
    }

    $r = Invoke-Curl -CurlArgs @(
        "-X", "POST", "$BaseUrl/api/commerce/process",
        "-H", "Content-Type: text/plain",
        "--data-binary", "@$(Join-Path $root 'process\process-01112026.json')"
    )
    Assert-Status $r 415 "process wrong content type"

    $r = Invoke-Process (Join-Path $root "process\process-malformed.json")
    Assert-Status $r 400 "process malformed json"

    # ------------------------------------------------------------------
    # 9. Errores de proceso
    # ------------------------------------------------------------------
    $processErrors = @(
        @{ Name = "empty"; Path = "process\process-empty.json"; Detail = "La fecha de proceso es obligatoria." },
        @{ Name = "default"; Path = "process\process-default.json"; Detail = "La fecha de proceso es obligatoria." },
        @{ Name = "null"; Path = "process\process-null.json"; Detail = $null },
        @{ Name = "invalid-format"; Path = "process\process-invalid-format.json"; Detail = $null }
    )

    foreach ($case in $processErrors) {
        $r = Invoke-Process (Join-Path $root $case.Path)
        Assert-Status $r 400 "process error: $($case.Name)"
        if ($null -ne $case.Detail) {
            Assert-Detail $r $case.Detail "process error detail: $($case.Name)"
        }
    }

    $r = Invoke-Process (Join-Path $root "process\process-no-data.json")
    Assert-Status $r 200 "process no data"
    Assert-Json $r.Json "quarantined" 0 "process no data quarantined"
    Assert-Json $r.Json "remaining" 0 "process no data remaining"

    # ------------------------------------------------------------------
    # 10. Errores de consulta
    # ------------------------------------------------------------------
    $r = Invoke-Get "page=0"
    Assert-Status $r 400 "get page 0"

    $r = Invoke-Get "page=-1"
    Assert-Status $r 400 "get page negative"

    $r = Invoke-Get "pageSize=0"
    Assert-Status $r 400 "get pageSize 0"

    $r = Invoke-Get "pageSize=101"
    Assert-Status $r 400 "get pageSize 101"

    $r = Invoke-Get "processDate=2026-13-40"
    Assert-Status $r 400 "get invalid date"

    $longSearch = ('a' * 101) -join ''
    $r = Invoke-Get "search=$longSearch&page=1&pageSize=10"
    Assert-Status $r 400 "get search too long"

    $r = Invoke-Get "processDate=2026-11-02&page=9999&pageSize=10"
    Assert-Status $r 200 "get page beyond total"
    Assert-Equal 0 $r.Json.items.Count "get page beyond total item count"

    # ------------------------------------------------------------------
    # 11. CORS
    # ------------------------------------------------------------------
    $corsOutput = & curl.exe -s -i -X OPTIONS "$BaseUrl/api/commerce/upload" `
        -H "Origin: http://localhost:4200" `
        -H "Access-Control-Request-Method: POST" `
        -H "Access-Control-Request-Headers: content-type"
    $corsText = ($corsOutput | Out-String)

    if ($corsText -match "HTTP/1\.[01] 204" -and
        $corsText -match "Access-Control-Allow-Origin: http://localhost:4200") {
        Write-Pass "CORS preflight"
    }
    else {
        Write-Fail "CORS preflight" $corsText
    }
}
catch {
    Write-Fail "error inesperado" $_.Exception.Message
}
finally {
    Write-Host ""
    Write-Host "Resultado: $script:Passes PASS, $script:Failures FAIL" -ForegroundColor Cyan
}

if ($script:Failures -gt 0) {
    exit 1
}
