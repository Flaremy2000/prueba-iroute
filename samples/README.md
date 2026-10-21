# Samples de prueba de la API

Estos archivos cubren los casos de éxito y de error de los endpoints de comercio:

- `POST /api/auth/login`
- `GET /api/auth/me`
- `POST /api/commerce/upload`
- `POST /api/commerce/process`
- `GET /api/commerce/quarantine`

Todos los endpoints de comercio requieren `Authorization: Bearer <token>`. Las fechas usadas son `2026-11-01` a `2026-11-09` para no chocar con el seed de `2026-10-20`.

## Requisitos

- API ejecutándose en `http://localhost:5238` (perfil `http`).
- SQL Server Express con la base `IRouteCommerce` y los scripts de `db/` aplicados, incluido `db/06_auth.sql`.
- `curl.exe` y `sqlcmd` disponibles en el PATH.
- Usuario de desarrollo `admin` / `Admin123!`.

## Limpieza de datos de prueba

Antes de repetir una corrida, elimina las fechas de los samples:

```powershell
sqlcmd -S ".\SQLEXPRESS" -d IRouteCommerce -E -C -Q "DELETE FROM dbo.commerce WHERE pc_processdate BETWEEN '2026-11-01' AND '2026-11-09'; DELETE FROM dbo.commerce_quarantine WHERE pc_processdate BETWEEN '2026-11-01' AND '2026-11-09'; DELETE FROM dbo.commerce_uploads WHERE process_date BETWEEN '2026-11-01' AND '2026-11-09';"
```

El script automático hace esta limpieza antes de probar, salvo que se use `-SkipReset`.

## Estructura

```text
samples/
├── upload/
│   ├── success/            # 4 filas válidas
│   ├── mixed/              # válidas + inválidas
│   ├── all-invalid/        # todas inválidas
│   ├── spaces/             # nombres/documentos con espacios
│   ├── semicolon/          # delimitador ;
│   ├── timestamp/          # pc_processdate con hora
│   └── errors/             # un caso de error por carpeta
├── process/                # bodies JSON para POST /process
├── run-api-tests.ps1       # ejecuta todos los casos
└── README.md
```

## 1. Carga y proceso: casos de éxito

| Sample | Archivo | Upload | Process | Resultado |
|---|---|---|---|---|
| Éxito total | `upload/success/commerce_01112026.csv` | 4 filas leídas / 4 insertadas | 0 en cuarentena / 4 válidas | No hay registros en cuarentena |
| Mixto | `upload/mixed/commerce_02112026.csv` | 7 / 7 | 5 / 2 | 5 van a cuarentena con motivo |
| Todas inválidas | `upload/all-invalid/commerce_07112026.csv` | 3 / 3 | 3 / 0 | 3 van a cuarentena |
| Espacios | `upload/spaces/commerce_09112026.csv` | 3 / 3 | 2 / 1 | Los campos con solo espacios quedan inválidos |
| Delimitador `;` | `upload/semicolon/commerce_04112026.csv` | 2 / 2 | 0 / 2 | Se detecta el delimitador automáticamente |
| Fecha con hora | `upload/timestamp/commerce_05112026.csv` | 2 / 2 | 0 / 2 | `2026-11-05 00:00:00` se normaliza a fecha |

### Detalle del sample mixto

`commerce_02112026.csv` genera 5 registros en cuarentena:

| Fila | Problema | Motivo |
|---|---|---|
| 2 | Nombre vacío | `El nombre del comercio (pc_nomcomred) se encuentra vacío` |
| 3 | Documento vacío | `El número de documento (pc_numdoc) se encuentra vacío` |
| 4 | Documento con letras (`20AB02`) | `El número de documento (pc_numdoc) contiene letras o caracteres especiales` |
| 5 | Documento con símbolo (`20-002`) | `El número de documento (pc_numdoc) contiene letras o caracteres especiales` |
| 6 | Nombre con solo espacios | `El nombre del comercio (pc_nomcomred) se encuentra vacío` |

Las filas 1 (`Comercio Valido`) y 7 (`Comercio Valido Dos`) quedan válidas.

## 2. Errores de carga

Todos estos samples deben devolver `400 Bad Request` y no deben insertar filas.

| Carpeta | Archivo | `detail` esperado |
|---|---|---|
| `empty` | `commerce_03112026.csv` (0 bytes) | `Debes enviar un archivo CSV no vacío.` |
| `headers-only` | `commerce_03112026.csv` | `El archivo no contiene registros.` |
| `invalid-file-name` | `comercio_03112026.csv` | `El archivo debe llamarse commerce_DDMMYYYY.csv con una fecha válida.` |
| `invalid-date-in-name` | `commerce_32132026.csv` | `El archivo debe llamarse commerce_DDMMYYYY.csv con una fecha válida.` |
| `missing-name-column` | `commerce_03112026.csv` | `El archivo no contiene las columnas obligatorias: pc_nomcomred.` |
| `missing-doc-column` | `commerce_03112026.csv` | `El archivo no contiene las columnas obligatorias: pc_numdoc.` |
| `missing-date-column` | `commerce_03112026.csv` | `El archivo no contiene las columnas obligatorias: pc_processdate.` |
| `date-mismatch` | `commerce_03112026.csv` | `El archivo contiene registros con fecha 2026-11-04, pero el nombre indica 2026-11-03.` |
| `invalid-date-cell` | `commerce_03112026.csv` | `La fila 2 tiene una fecha de proceso inválida: '2026-13-40'.` |
| `malformed-csv` | `commerce_03112026.csv` | `La fila 2 tiene datos inválidos.` |
| `empty-process-date` | `commerce_06112026.csv` | `La fila 2 tiene una fecha de proceso inválida: ''.` |
| `long-name` | `commerce_08112026.csv` | `La fila 2 tiene un nombre de comercio mayor a 200 caracteres.` |
| `long-doc` | `commerce_08112026.csv` | `La fila 2 tiene un número de documento mayor a 50 caracteres.` |
| `duplicate-headers` | `commerce_08112026.csv` | `El archivo contiene columnas duplicadas: pc_numdoc.` |

> Los nombres de archivo son intencionales: no los renombres al probar. El nombre `commerce_DDMMYYYY.csv` y la fecha de la columna son parte de la validación.

### Errores HTTP y de transporte

Además de los CSV, el runner comprueba casos que no dependen de un archivo de datos:

| Caso | Petición | Resultado esperado |
|---|---|---|
| Método no permitido | `GET /api/commerce/upload` | `405` |
| Tipo de contenido inválido en upload | `POST /api/commerce/upload` con `application/json` | `415` |
| Archivo faltante | `POST /api/commerce/upload` multipart sin parte `file` | `400`, `Debes enviar un archivo CSV no vacío.` |
| Archivo mayor a 20 MB | CSV temporal de 21 MB | `400`, `Request body too large.` |
| Sin autenticación | `GET /api/commerce/quarantine` sin token | `401`, `No autorizado` |
| Login inválido | `POST /api/auth/login` con contraseña incorrecta | `400`, `Usuario o contraseña inválidos.` |
| CSV duplicado | Subir dos veces el mismo contenido | `409`, `El archivo ya fue cargado. No se insertaron registros duplicados.` |
| Tipo de contenido inválido en process | `POST /api/commerce/process` con `text/plain` | `415` |
| JSON malformado | `process-malformed.json` | `400` |
| Búsqueda demasiado larga | `search` de 101 caracteres | `400` |
| Página fuera de rango | `page=9999` | `200` con `items = []` |

## 3. Proceso por fecha

| Body | Caso | Resultado esperado |
|---|---|---|
| `process-01112026.json` | Después del sample de éxito | `200`, `quarantined = 0`, `remaining = 4` |
| `process-02112026.json` | Después del sample mixto | `200`, `quarantined = 5`, `remaining = 2` |
| `process-04112026.json` | Después del sample con `;` | `200`, `quarantined = 0`, `remaining = 2` |
| `process-05112026.json` | Después del sample con hora | `200`, `quarantined = 0`, `remaining = 2` |
| `process-07112026.json` | Después del sample de solo inválidos | `200`, `quarantined = 3`, `remaining = 0` |
| `process-09112026.json` | Después del sample de espacios | `200`, `quarantined = 2`, `remaining = 1` |
| `process-empty.json` | Body `{}` | `400`, `La fecha de proceso es obligatoria.` |
| `process-null.json` | `processDate: null` | `400` de validación de JSON |
| `process-invalid-format.json` | `2026-13-40` | `400` de validación de JSON |
| `process-default.json` | `0001-01-01` | `400`, `La fecha de proceso es obligatoria.` |
| `process-no-data.json` | Fecha sin registros | `200`, `quarantined = 0`, `remaining = 0` |

El proceso es idempotente: si se vuelve a procesar la misma fecha, los registros inválidos ya no están en `commerce` y no se vuelven a mover. Por eso una segunda ejecución del sample mixto devuelve `0 / 2`.

## 4. Consulta de cuarentena

Después de procesar `2026-11-02`:

| Query | Resultado esperado |
|---|---|
| `?processDate=2026-11-02&page=1&pageSize=10` | `200`, `total = 5`, 5 items |
| `?processDate=2026-11-02&search=Letras&page=1&pageSize=10` | `200`, `total = 1`, `Comercio Con Letras` |
| `?processDate=2026-11-02&search=20-002&page=1&pageSize=10` | `200`, `total = 1`, `Comercio Con Guion` |
| `?processDate=2026-11-02&page=2&pageSize=2` | `200`, `total = 5`, 2 items |
| `?processDate=2026-12-31` | `200`, `total = 0` |
| `?page=0` | `400` de validación |
| `?pageSize=101` | `400` de validación |
| `?processDate=2026-13-40` | `400` de validación |

## 5. Ejecución automática

Con la API levantada:

```powershell
.\samples\run-api-tests.ps1
```

Si la API está en otro puerto:

```powershell
.\samples\run-api-tests.ps1 -BaseUrl "http://localhost:5239"
```

El script:

- Limpia los datos de `2026-11-01` a `2026-11-09`, incluyendo `commerce_uploads` (se puede omitir con `-SkipReset`).
- Verifica que la API responda.
- Inicia sesión con `admin` / `Admin123!` y comprueba credenciales inválidas, `401` y `429`.
- Ejecuta los uploads, procesos y consultas.
- Comprueba el `409` al subir dos veces el mismo CSV.
- Comprueba estados HTTP, conteos, motivos, búsqueda, paginación, CORS e idempotencia.
- Incluye casos de transporte: `405`, `413`, `415`, archivo faltante y JSON malformado.
- Devuelve código de salida `1` si algún caso falla.

## 6. Comandos manuales

```powershell
# Iniciar sesión
$login = curl.exe -s -X POST "http://localhost:5238/api/auth/login" -H "Content-Type: application/json" --data-binary '{"username":"admin","password":"Admin123!"}' | ConvertFrom-Json
$token = $login.accessToken

# Subir un sample
curl.exe -s -X POST "http://localhost:5238/api/commerce/upload" -H "Authorization: Bearer $token" -F "file=@samples/upload/success/commerce_01112026.csv"

# Repetir la carga: debe devolver 409
curl.exe -s -X POST "http://localhost:5238/api/commerce/upload" -H "Authorization: Bearer $token" -F "file=@samples/upload/success/commerce_01112026.csv"

# Procesar una fecha
curl.exe -s -X POST "http://localhost:5238/api/commerce/process" -H "Authorization: Bearer $token" -H "Content-Type: application/json" --data-binary "@samples/process/process-01112026.json"

# Consultar cuarentena
curl.exe -s "http://localhost:5238/api/commerce/quarantine?processDate=2026-11-02&page=1&pageSize=10" -H "Authorization: Bearer $token"
```
