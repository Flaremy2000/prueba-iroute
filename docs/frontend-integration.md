# Arquitectura e integración del frontend Angular

Este documento describe la arquitectura técnica del frontend de i-Route, cómo se comunica con la API de .NET y cómo están estructurados sus componentes, servicios y flujos de negocio.

---

## 1. Stack y diseño técnico

- **Framework**: Angular 21 (Componentes Standalone, sin `NgModule`).
- **Gestión de estado**: Reactividad nativa con **Angular Signals** (`signal`, `computed`).
- **Estrategia de detección de cambios**: `ChangeDetectionStrategy.OnPush` en todos los componentes para minimizar ciclos de verificación y maximizar el rendimiento.
- **Estilos**: Tailwind CSS con paleta moderna, componentes accesibles y diseño responsivo.
- **Rutas y SSR**: Angular Router con lazy loading (`loadComponent`) y soporte para renderizado en servidor (SSR con Hydration).
- **Pruebas**: Vitest (`ng test`) con 13 suites y 73 pruebas unitarias automatizadas.

---

## 2. Estructura del proyecto

La aplicación sigue una organización modular guiada por dominios y responsabilidades limpias en `src/app/`:

```
src/app/
├── core/                         # Núcleo técnico y servicios compartidos
│   ├── auth/                     # Autenticación, guards e interceptores
│   │   ├── auth.service.ts       # Login, sesión en sessionStorage y decodificación de expiración
│   │   ├── auth.interceptor.ts   # Inyección automática de Bearer token y manejo de 401
│   │   └── auth.guard.ts         # authGuard (protección) y guestGuard (redirección)
│   ├── commerce/                 # Capa de API de negocio
│   │   └── commerce.service.ts   # Endpoints de carga, procesamiento y cuarentena
│   ├── models/                   # Interfaces TypeScript fuertemente tipadas
│   │   └── models.ts             # DTOs de login, carga, proceso, cuarentena y preview
│   ├── utils/                    # Funciones puras y testeables
│   │   ├── csv-file.ts           # Validación de archivo y lectura de previsualización
│   │   ├── date.ts               # Manipulación DateOnly (ISO) sin desfase horario
│   │   └── problem-details.ts    # Parseo amigable de errores RFC 9110
│   └── index.ts                  # Barrel export unificado
├── features/                     # Vistas funcionales de la aplicación
│   ├── auth/
│   │   └── login/                # Pantalla de inicio de sesión
│   └── commerce/
│       ├── upload/               # Pantalla de carga y previsualización de CSV
│       ├── process/              # Pantalla de procesamiento por fecha
│       └── quarantine/           # Pantalla de consulta de registros en cuarentena
└── layout/                       # Componentes estructurales
    └── shell/                    # Barra lateral, navegación activa, cabecera y responsive
```

---

## 3. Autenticación y seguridad

### 3.1 Flujo de inicio de sesión
1. El usuario introduce sus credenciales en `/login`.
2. `AuthService.login()` envía un `POST /api/auth/login`.
3. El token JWT y los datos del usuario se guardan en `sessionStorage` (`iroute.token` e `iroute.user`).
4. Si la autenticación es exitosa, se redirige a `/upload` (o al `returnUrl` solicitado, previa validación de seguridad contra redirecciones abiertas).

### 3.2 Interceptor HTTP (`authInterceptor`)
- Se ejecuta en todas las peticiones salientes.
- Si la petición va dirigida a la API (`environment.apiUrl`) y existe un token activo, adjunta automáticamente:
  ```http
  Authorization: Bearer <access_token>
  ```
- **Optimización de seguridad**: No adjunta cabecera de autorización a la solicitud de login (`/api/auth/login`).
- Si la API responde con `401 Unauthorized` en cualquier endpoint protegido, limpia la sesión y redirige inmediatamente al login.

### 3.3 Guards de navegación
- `authGuard`: Bloquea el acceso a `/upload`, `/process` y `/quarantine` a usuarios no autenticados, redirigiendo a `/login?returnUrl=...`.
- `guestGuard`: Si un usuario con sesión activa intenta entrar a `/login`, lo envía automáticamente a `/upload`.

### 3.4 Consideraciones de seguridad (XSS y Open Redirect)
- **Mitigación XSS**: Uso de `sessionStorage` (aislado a la pestaña activa y destruido al cerrarla) junto con el contextual escaping nativo de Angular en todos los templates. Sin uso de `innerHTML` ni derivaciones inseguras de `DomSanitizer`.
- **Mitigación Open Redirect**: `LoginPage` valida que el `returnUrl` comience por `/` y no por `//` ni redirija a `/login`.

---

## 4. Flujo de negocio de comercios

### 4.1 Carga de archivos y previsualización local (`/upload`)
- **Validación local inmediata**:
  - Nombre estricto: `commerce_DDMMYYYY.csv`.
  - Fecha del nombre válida (ej. `31022026` es rechazada).
  - Tamaño máximo de 20 MB y archivo no vacío.
- **Previsualización de contenido en el navegador**:
  - Antes del envío, `readCsvPreview()` lee un fragmento inicial de 64 KB con `file.slice()` para no bloquear la interfaz en archivos pesados.
  - Parsea delimitadores coma (`,`) o punto y coma (`;`), limpia comillas y extrae las cabeceras `pc_nomcomred`, `pc_numdoc` y `pc_processdate`.
  - Renderiza una tabla con las primeras filas de muestra para verificación del usuario.
- **Envío al backend**:
  - Envío mediante `FormData` a `POST /api/commerce/upload` sin sobrescribir manualmente el `Content-Type` (dejando que el navegador configure el boundary).
  - Manejo de error `409 Conflict` cuando el contenido ya fue cargado previamente (deduplicación por SHA-256).
  - Al completar la carga, muestra el total de filas leídas/insertadas y un botón directo para procesar esa misma fecha.

### 4.2 Procesamiento por fecha (`/process`)
- Recibe opcionalmente el parámetro `?date=YYYY-MM-DD` desde la pantalla de carga.
- Invoca `POST /api/commerce/process` con `{ processDate: "YYYY-MM-DD" }`.
- Muestra el resultado devuelto por el stored procedure del backend:
  - `quarantined`: cantidad de registros inválidos movidos a cuarentena.
  - `remaining`: cantidad de registros válidos que permanecen en la tabla de comercios.
- Enlace directo para consultar los registros en cuarentena de esa fecha específica.

### 4.3 Consulta de cuarentena (`/quarantine`)
- Consulta mediante `GET /api/commerce/quarantine` con los parámetros:
  - `processDate`: filtro por fecha específica (opcional).
  - `search`: búsqueda textual sobre nombre de comercio o documento (opcional).
  - `page` y `pageSize`: paginación configurable (10, 20, 50, 100 registros).
- **Optimizaciones de experiencia de usuario**:
  - Búsqueda con operador `debounceTime(400)` y `distinctUntilChanged` para evitar llamadas redundantes mientras el usuario escribe.
  - Reinicio automático a la página 1 cuando cambia cualquier filtro.
  - Skeletons de carga durante la espera de datos.
  - Visualización del motivo de rechazo en cada fila y formateo amigable de valores nulos.

---

## 5. Manejo de errores (`ProblemDetails`)

Todas las respuestas de error de la API siguen el estándar RFC 9110 (`ProblemDetails`). La función [`problemMessage()`](file:///D:/Projects/prueba-iroute/frontend/src/app/core/utils/problem-details.ts) extrae el mensaje de mayor prioridad:
1. Campo `detail` devuelto por el backend.
2. Campo `title` si `detail` no está presente.
3. Mensajes específicos para `401` (sesión expirada), `429` (demasiados intentos / rate limit) y `0` (API no disponible / desconectada).
4. Mensaje alternativo (*fallback*) en caso de error inesperado.

---

## 6. Pruebas y compilación

```powershell
# Ejecutar tests unitarios (Vitest)
npm test

# Verificación de tipos estricta sin emitir archivos
npx tsc --noEmit

# Compilación de producción (cliente + SSR)
npm run build
```
