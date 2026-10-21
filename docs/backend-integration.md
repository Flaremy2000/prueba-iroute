# Arquitectura del Backend .NET

Documento de referencia técnica sobre la implementación de la API Web construida en .NET 10 para i-Route.

---

## 1. Stack y diseño técnico

- **Plataforma**: .NET 10 / C# 14.
- **Framework Web**: ASP.NET Core Web API (Controllers tipados, validación declarativa).
- **Acceso a datos**:
  - Entity Framework Core para consultas de lectura y modelos relacionales.
  - Invocación de Stored Procedures y Table-Valued Parameters (TVP) mediante Dapper / ADO.NET para inserción masiva y procesamiento atómico.
- **Seguridad**:
  - Autenticación mediante JSON Web Tokens (JWT) con validación estricta de emisor, audiencia y expiración (60 min).
  - Hashing de contraseñas con PBKDF2-SHA256, 210.000 iteraciones y sal criptográfica de 16 bytes.
  - Rate limiting nativo en el endpoint de login (5 solicitudes por minuto por IP con respuesta `429 Too Many Requests`).
- **Manejo de errores**: `GlobalExceptionHandler` que transforma excepciones en respuestas estándar RFC 9110 (`ProblemDetails`) con `traceId` para observabilidad.
- **Documentación interactiva**: OpenAPI / Scalar disponible en `/scalar/v1` y especificación JSON en `/openapi/v1.json`.

---

## 2. Organización del código

La solución sigue principios de Arquitectura Limpia (*Clean Architecture*) y separación de responsabilidades:

```
backend/
├── Api/                          # Capa de presentación y controladores
│   ├── Configuration/            # Extensiones modulares de inyección de dependencias
│   │   ├── ApiServiceCollectionExtensions.cs
│   │   ├── AuthenticationConfiguration.cs
│   │   ├── CorsConfiguration.cs
│   │   ├── OpenApiConfiguration.cs
│   │   ├── RateLimitingConfiguration.cs
│   │   └── WebApplicationExtensions.cs
│   ├── Contracts/                # Modelos de entrada y salida (DTOs de la API)
│   ├── Controllers/              # Controladores REST (AuthController, CommerceController)
│   └── Middleware/               # Manejador global de excepciones y eventos JWT
├── Application/                  # Capa de aplicación y lógica de negocio
│   ├── Abstractions/             # Interfaces de repositorios y servicios (DIP)
│   ├── Auth/                     # Casos de uso de autenticación y generación de tokens
│   ├── Commerce/                 # Carga, deduplicación y procesamiento de comercios
│   ├── Common/                   # Modelos comunes y resultados de parseo
│   └── Quarantine/               # Consultas y paginación de registros rechazados
├── Infrastructure/               # Capa de infraestructura y acceso a datos
│   ├── Auth/                     # Implementación de hash PBKDF2 y TokenService
│   ├── Csv/                      # Parser de archivos CSV con soporte de delimitadores y BOM
│   ├── Data/                     # DbContext de EF Core y mapeos relacionales
│   └── Repositories/             # Repositorios de comercios y usuarios (SPs + consultas)
└── Program.cs                    # Punto de entrada y composición del pipeline
```

---

## 3. Endpoints expuestos

### 3.1 Autenticación (`/api/auth`)

| Método | Ruta | Descripción | Seguridad |
|---|---|---|---|
| `POST` | `/api/auth/login` | Autentica usuario y contraseña, devuelve token JWT | Anónimo (Rate limited: 5/min) |
| `GET` | `/api/auth/me` | Devuelve el nombre y rol del usuario autenticado | Requiere Bearer Token |

### 3.2 Comercio (`/api/commerce`)

Todos los endpoints requieren `Authorization: Bearer <access_token>`.

| Método | Ruta | Descripción | Respuestas clave |
|---|---|---|---|
| `POST` | `/api/commerce/upload` | Carga un archivo CSV `commerce_DDMMYYYY.csv` mediante `multipart/form-data` | `200` Éxito, `400` Archivo inválido/vacío, `409` Archivo duplicado (SHA-256) |
| `POST` | `/api/commerce/process` | Procesa los comercios de una fecha y mueve los inválidos a cuarentena | `200` Conteo de válidos y en cuarentena, `400` Fecha inválida |
| `GET` | `/api/commerce/quarantine` | Consulta registros en cuarentena con filtros y paginación | `200` Resultado paginado (`items`, `total`, `page`, `pageSize`) |

---

## 4. Aspectos clave de rendimiento y resiliencia

### 4.1 Deduplicación atómica de archivos (SHA-256)
- Mientras el stream del CSV se procesa, se computa su hash SHA-256 en memoria de forma continua.
- El procedimiento almacenado `sp_create_commerce` verifica en `commerce_uploads` con bloqueos `UPDLOCK, HOLDLOCK` si el hash ya existe.
- Si el archivo ya fue cargado (incluso con otro nombre), la transacción no inserta filas y la API responde con `409 Conflict`.

### 4.2 Inserción masiva con Table-Valued Parameters (TVP)
- Los registros validados del CSV no se insertan fila por fila (`INSERT` individual).
- Se utiliza el tipo tabla de SQL Server `dbo.CommerceTvp`, permitiendo enviar miles de filas en un único viaje de red (*round-trip*) a la base de datos.

### 4.3 Procesamiento idempotente por fecha
- `sp_process_commerce` ejecuta la validación, inserción en cuarentena con `motivo` y eliminación de la tabla principal en una sola transacción atómica.
- Si se ejecuta múltiples veces para la misma fecha, el resultado es idempotente: no duplica registros en cuarentena ni altera los registros válidos restantes.

---

## 5. Pruebas y cobertura

El proyecto `backend.Tests` (`IRoute.Tests.csproj`) contiene pruebas unitarias, de integración y de API:

```powershell
# Ejecución de la suite completa de pruebas
dotnet test backend.Tests/IRoute.Tests.csproj -c Debug
```

- **Total de pruebas**: 72 pruebas automatizadas (0 fallos).
- **Cobertura**: Parser de CSV (delimitadores, BOM, longitudes), servicios de comandos, consultas, hashing PBKDF2, JWT, rate limiting y pruebas de integración con stored procedures en SQL Server.
