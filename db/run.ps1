[CmdletBinding()]
param(
    [string]$Server = ".\SQLEXPRESS",
    [switch]$Seed
)

$ErrorActionPreference = "Stop"

if (-not (Get-Command sqlcmd -ErrorAction SilentlyContinue)) {
    throw "No se encontró sqlcmd. Instala las herramientas de SQL Server o ejecuta los scripts manualmente."
}

$scripts = @(
    "00_create_database.sql",
    "01_schema.sql",
    "02_alter_commerce_quarantine.sql",
    "03_stored_procedures.sql"
)

if ($Seed) {
    $scripts += "04_seed.sql"
}

foreach ($script in $scripts) {
    $path = Join-Path $PSScriptRoot $script
    Write-Host ">> $script"

    & sqlcmd -S $Server -E -C -b -f 65001 -i $path

    if ($LASTEXITCODE -ne 0) {
        throw "Falló la ejecución de $script (código $LASTEXITCODE)."
    }
}

Write-Host "Base de datos lista en $Server."
