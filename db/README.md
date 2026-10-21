# Base de datos

Scripts de SQL Server para la prueba técnica.

## Archivos

| Archivo | Responsabilidad |
|---|---|
| `00_create_database.sql` | Crea la base `IRouteCommerce` si no existe |
| `01_schema.sql` | Tablas, índices y tipo TVP |
| `02_alter_commerce_quarantine.sql` | Agrega la columna `motivo` a `commerce_quarantine` |
| `03_stored_procedures.sql` | `sp_create_commerce` y `sp_process_commerce` |
| `04_seed.sql` | Datos de prueba opcionales |
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
sqlcmd -S ".\SQLEXPRESS" -E -C -b -i db\04_seed.sql
```

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
