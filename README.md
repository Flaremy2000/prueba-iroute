# i-Route — Consola de Ingesta y Procesamiento de Comercios

Solución fullstack para la carga, previsualización, procesamiento y control de calidad de archivos de comercios (`commerce_DDMMYYYY.csv`), con segregación automática a cuarentena de registros inconsistentes.

---

## Stack Tecnológico

| Capa | Tecnología | Descripción |
|---|---|---|
| **Frontend** | Angular 21 | Componentes standalone, Signals, `OnPush`, Tailwind CSS y soporte SSR |
| **Backend** | .NET 10 / ASP.NET Core | Arquitectura Limpia, Stored Procedures, JWT (PBKDF2), Rate Limiting |
| **Base de Datos** | SQL Server | Transacciones atómicas, Table-Valued Parameters (TVP), índices dedicados |

---

## Estructura del Repositorio

```text
├── backend/            # API REST en ASP.NET Core (.NET 10)
├── backend.Tests/      # Suite de pruebas unitarias, integración y API (xUnit)
├── frontend/           # Aplicación Angular 21 (Standalone, Signals)
├── db/                 # Scripts SQL Server (tablas, SPs, usuarios y runner)
├── docs/               # Documentación técnica detallada
│   ├── backend-integration.md  # Arquitectura, endpoints y seguridad del API
│   ├── frontend-integration.md # Arquitectura del cliente, estado y previsualización
│   └── database-integration.md # Modelo relacional, SPs y tipos TVP
└── samples/            # Archivos CSV de ejemplo y script de pruebas end-to-end
```

---

## Requisitos Previos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js 20+](https://nodejs.org/) y npm
- SQL Server (LocalDB o instancia SQL Server Express)
- Herramienta de línea de comandos `sqlcmd` (incluida en SQL Server Tools)

---

## Puesta en Marcha

### 1. Base de datos
Ejecuta el script automatizado para crear la base de datos `IRouteCommerce`, sus tablas, procedimientos almacenados y el usuario inicial:

```powershell
# Para instancia SQL Server Express (por defecto):
.\db\run.ps1 -Server ".\SQLEXPRESS"

# O si utilizas LocalDB:
.\db\run.ps1 -Server "(localdb)\MSSQLLocalDB"
```

> **Credenciales de desarrollo**: Usuario `admin` | Contraseña `Admin123!`

### 2. Backend (.NET 10)
Levanta la API con el perfil HTTP:

```powershell
dotnet run --project backend --launch-profile http
```

- **API Base**: `http://localhost:5238`
- **Documentación interactiva (Scalar)**: `http://localhost:5238/scalar/v1`
- **Contrato OpenAPI**: `http://localhost:5238/openapi/v1.json`

### 3. Frontend (Angular 21)
Instala dependencias y corre el servidor de desarrollo:

```powershell
cd frontend
npm install
npm start
```

- **Aplicación web**: `http://localhost:4200`

---

## Flujo Funcional

1. **Autenticación (`/login`)**:
   Inicio de sesión protegido contra fuerza bruta (rate limit de 5 intentos/min por IP) y mitigación de open-redirect. El token se mantiene aislado en `sessionStorage`.
2. **Carga y Previsualización (`/upload`)**:
   Carga mediante selector o drag-and-drop de archivos `commerce_DDMMYYYY.csv`. Valida nombre, fecha y tamaño (< 20 MB). **Lee y previsualiza los datos en pantalla antes del envío**. La API previene cargas duplicadas mediante hash SHA-256 atómico.
3. **Procesamiento de Fecha (`/process`)**:
   Ejecuta las validaciones de negocio en base de datos (`sp_process_commerce`). Separa registros válidos de inconsistentes (nombres vacíos o documentos con caracteres no numéricos) en una sola transacción.
4. **Consulta de Cuarentena (`/quarantine`)**:
   Tabla reactiva paginada con selector de tamaño de página, filtro por fecha y búsqueda con debounce (400 ms) para consultar los registros rechazados y su `motivo` detallado.

---

## Pruebas Automatizadas

### Pruebas de Backend (72 tests)
Pruebas de controladores, repositorios, parser CSV, seguridad PBKDF2 y procedimientos almacenados en base de datos:

```powershell
dotnet test backend.Tests/IRoute.Tests.csproj -c Debug
```

### Pruebas de Frontend (73 tests)
Pruebas de componentes, servicios, guards, interceptores y utilitarios con Vitest:

```powershell
cd frontend
npm test
```

### Pruebas de Integración con Samples
Prueba integral de la API con los archivos CSV de muestra:

```powershell
.\samples\run-api-tests.ps1
```

---

## Documentación Detallada

Para profundizar en la implementación de cada capa, consulta los documentos técnicos en `docs/`:
- [Arquitectura del Backend](docs/backend.md)
- [Arquitectura del Frontend](docs/frontend-integration.md)
- [Arquitectura de Base de Datos](docs/database.md)
