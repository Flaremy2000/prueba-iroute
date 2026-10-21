# Base de datos

Scripts de SQL Server para la prueba técnica.

## Archivos

| Archivo | Responsabilidad |
|---|---|
| `00_create_database.sql` | Crea la base `IRouteCommerce` si no existe |
| `01_schema.sql` | Tablas, índices y tipo TVP |
| `02_alter_commerce_quarantine.sql` | Agrega la columna `motivo` a `commerce_quarantine` |
| `03_stored_procedures.sql` | `sp_create_commerce` (con deduplicación por hash) y `sp_process_commerce` |
| `04_seed.sql` | Datos de prueba opcionales |
| `06_auth.sql` | Tabla `users` y usuario `admin` de desarrollo |
| `run.ps1` | Ejecuta los scripts en orden |

Cada archivo tiene una sola responsabilidad y es idempotente. Así se puede revisar, ejecutar y corregir un paso sin repetir todo el esquema.

## Ejecución

Contra SQL Server Express:

```powershell
.\db\run.ps1
```

Con datos de prueba:

```powershell
.\db\run.ps1 -Seed
```

Contra otra instancia:

```powershell
.\db\run.ps1 -Server ".\SQLEXPRESS"
```

El script usa `-C` porque el certificado del servidor local no es de confianza.

## Ejecución manual

```powershell
sqlcmd -S ".\SQLEXPRESS" -E -C -b -i db\00_create_database.sql
sqlcmd -S ".\SQLEXPRESS" -E -C -b -i db\01_schema.sql
sqlcmd -S ".\SQLEXPRESS" -E -C -b -i db\02_alter_commerce_quarantine.sql
sqlcmd -S ".\SQLEXPRESS" -E -C -b -i db\03_stored_procedures.sql
sqlcmd -S ".\SQLEXPRESS" -E -C -b -i db\06_auth.sql
sqlcmd -S ".\SQLEXPRESS" -E -C -b -i db\04_seed.sql
```

## Autenticación

`06_auth.sql` crea la tabla `users` y el usuario de desarrollo:

| Usuario | Contraseña | Rol |
|---|---|---|
| `admin` | `Admin123!` | `Admin` |

La contraseña se guarda con PBKDF2-SHA256, 210.000 iteraciones y sal aleatoria. Cambia estas credenciales antes de desplegar. El backend valida el JWT con `Jwt:Key`; en producción debes configurarla por variable de entorno o user secrets, nunca versionarla.

## Deduplicación de cargas

`sp_create_commerce` recibe `@file_name`, `@content_hash` y `@process_date`. El backend calcula SHA-256 sobre el contenido binario del CSV y lo envía como `@content_hash`. La tabla `commerce_uploads` tiene un índice único sobre `content_hash`:

- Si el hash ya existe, el SP devuelve `duplicate = 1` y no inserta filas.
- La API responde `409 Conflict` con el detalle `El archivo ya fue cargado. No se insertaron registros duplicados.`
- El mismo contenido se considera duplicado aunque cambie el nombre del archivo.

## Prueba del proceso

Con los datos de `04_seed.sql`:

```sql
EXEC dbo.sp_process_commerce @pc_processdate = '2026-10-20';

SELECT id, pc_nomcomred, pc_numdoc, pc_processdate, motivo
FROM dbo.commerce_quarantine
ORDER BY id;
```

Resultado esperado:

- `quarantined = 5`
- `remaining = 2`

## Estructura asumida del CSV

Solo se conocen estas columnas:

- `pc_nomcomred`
- `pc_numdoc`
- `pc_processdate`

Si el CSV real trae más, hay que agregarlas a `commerce`, a `commerce_quarantine` y al tipo `dbo.CommerceTvp`.
