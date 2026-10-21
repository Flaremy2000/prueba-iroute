# Arquitectura de Base de Datos (SQL Server)

Documento de referencia sobre el modelo relacional, tipos de usuario, procedimientos almacenados y estrategia de rendimiento implementados en SQL Server para i-Route.

---

## 1. Esquema relacional

La base de datos `IRouteCommerce` contiene las siguientes tablas principales:

### 1.1 `dbo.commerce`
Almacena los registros cargados desde los archivos CSV antes o después de su procesamiento.

| Columna | Tipo | Nulo | Descripción |
|---|---|---|---|
| `id` | `BIGINT IDENTITY(1,1)` | No | Clave primaria autoincremental |
| `pc_nomcomred` | `NVARCHAR(200)` | Sí | Nombre o razón social del comercio |
| `pc_numdoc` | `NVARCHAR(50)` | Sí | Número de identificación o documento |
| `pc_processdate` | `DATE` | Sí | Fecha de proceso obtenida del CSV |

- **Índice**: `IX_commerce_pc_processdate` sobre `pc_processdate` para optimizar el filtrado durante la ejecución del proceso.

---

### 1.2 `dbo.commerce_quarantine`
Almacena los registros que no superaron las reglas de negocio durante el procesamiento.

| Columna | Tipo | Nulo | Descripción |
|---|---|---|---|
| `id` | `BIGINT IDENTITY(1,1)` | No | Clave primaria autoincremental |
| `pc_nomcomred` | `NVARCHAR(200)` | Sí | Nombre del comercio rechazado |
| `pc_numdoc` | `NVARCHAR(50)` | Sí | Documento del comercio rechazado |
| `pc_processdate` | `DATE` | Sí | Fecha en la que ocurrió el proceso |
| `motivo` | `NVARCHAR(400)` | No | Explicación detallada de las causas del rechazo |

- **Índice**: `IX_commerce_quarantine_pc_processdate` sobre `pc_processdate` para acelerar las consultas paginadas en la consola.
- **Origen de la columna `motivo`**: Se agrega mediante el script `02_alter_commerce_quarantine.sql` garantizando idempotencia.

---

### 1.3 `dbo.commerce_uploads`
Controla el historial de archivos cargados para impedir la inserción de contenido duplicado.

| Columna | Tipo | Nulo | Descripción |
|---|---|---|---|
| `id` | `BIGINT IDENTITY(1,1)` | No | Clave primaria |
| `file_name` | `NVARCHAR(255)` | No | Nombre del archivo original |
| `content_hash` | `CHAR(64)` | No | Hash SHA-256 en hexadecimal del contenido |
| `process_date` | `DATE` | No | Fecha asociada al archivo |
| `rows_inserted` | `INT` | No | Cantidad de filas insertadas |
| `uploaded_at` | `DATETIME2(0)` | No | Fecha/hora UTC de la carga |

- **Restricción**: `UQ_commerce_uploads_content_hash` índice único sobre `content_hash` que garantiza a nivel de motor la imposibilidad de procesar dos veces el mismo contenido.

---

### 1.4 `dbo.users`
Almacena las credenciales de usuarios para la autenticación en el sistema.

| Columna | Tipo | Nulo | Descripción |
|---|---|---|---|
| `id` | `BIGINT IDENTITY(1,1)` | No | Clave primaria |
| `username` | `NVARCHAR(100)` | No | Nombre de usuario (índice único) |
| `password_hash` | `NVARCHAR(255)` | No | Hash PBKDF2-SHA256 con sal |
| `role` | `NVARCHAR(50)` | No | Rol del usuario (`Admin`, `User`) |
| `created_at` | `DATETIME2(0)` | No | Fecha de creación del registro |

---

## 2. Tipos definidos por el usuario (TVP)

### `dbo.CommerceTvp`
Permite transferir colecciones completas de filas desde .NET hacia SQL Server en una sola llamada de red:

```sql
CREATE TYPE dbo.CommerceTvp AS TABLE
(
    pc_nomcomred   NVARCHAR(200) NULL,
    pc_numdoc      NVARCHAR(50)  NULL,
    pc_processdate DATE          NULL
);
```

---

## 3. Procedimientos Almacenados

### 3.1 `dbo.sp_create_commerce`
- **Propósito**: Insertar en bloque las filas validadas de un CSV y registrar el hash del archivo de forma atómica.
- **Mecanismo de deduplicación**:
  ```sql
  IF @content_hash IS NOT NULL
     AND EXISTS (SELECT 1 FROM dbo.commerce_uploads WITH (UPDLOCK, HOLDLOCK) WHERE content_hash = @content_hash)
  BEGIN
      SELECT 0 AS inserted, CAST(1 AS BIT) AS duplicate;
      RETURN;
  END;
  ```
- Devuelve `inserted` (filas insertadas) y `duplicate` (bandera booleana).

---

### 3.2 `dbo.sp_process_commerce`
- **Propósito**: Ejecutar las reglas de negocio sobre los registros de una fecha indicada (`@pc_processdate`).
- **Reglas de validación aplicadas**:
  1. `pc_nomcomred` vacío o compuesto únicamente de espacios:
     *Motivo: "El nombre del comercio (pc_nomcomred) se encuentra vacío"*
  2. `pc_numdoc` vacío o con espacios:
     *Motivo: "El número de documento (pc_numdoc) se encuentra vacío"*
  3. `pc_numdoc` con letras o caracteres especiales (`LIKE '%[^0-9]%'`):
     *Motivo: "El número de documento (pc_numdoc) contiene letras o caracteres especiales"*
- **Comportamiento atómico**:
  - Identifica los registros inválidos en una variable tipo tabla con `CONCAT_WS`.
  - Copia los registros inválidos a `dbo.commerce_quarantine` con su respectivo motivo.
  - Elimina los registros inválidos de `dbo.commerce`.
  - Devuelve en un único `SELECT`:
    - `@quarantined`: Total de registros movidos a cuarentena.
    - `@remaining`: Total de registros válidos que permanecen en `commerce`.

---

## 4. Scripts y ejecución

Los scripts SQL están organizados de forma secuencial e idempotente en la carpeta `db/`:

```
db/
├── 00_create_database.sql          # Creación de base de datos IRouteCommerce
├── 01_schema.sql                   # Tablas base, índices y tipo CommerceTvp
├── 02_alter_commerce_quarantine.sql # Agregado de columna motivo
├── 03_stored_procedures.sql        # sp_create_commerce y sp_process_commerce
├── 04_seed.sql                     # Datos opcionales de prueba
├── 06_auth.sql                     # Usuario de desarrollo (admin / Admin123!)
├── README.md                       # Documentación puntual de base de datos
└── run.ps1                         # Automatización de ejecución completa
```

### Ejecución automática vía PowerShell

```powershell
# Para instancia SQL Server Express (por defecto):
.\db\run.ps1 -Server ".\SQLEXPRESS"

# Para instancia LocalDB:
.\db\run.ps1 -Server "(localdb)\MSSQLLocalDB"
```
