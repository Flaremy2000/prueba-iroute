# Integración del frontend Angular

Documento de referencia para construir el frontend de i-Route contra la API actual.

## 1. URLs y configuración

| Entorno | API |
|---|---|
| Desarrollo | `http://localhost:5238` |
| Frontend permitido por CORS | `http://localhost:4200` |

En Angular, configura la URL en `src/environments/environment.ts`:

```ts
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5238'
};
```

La API acepta JSON y formularios `multipart/form-data`. Para los endpoints protegidos hay que enviar:

```http
Authorization: Bearer <access_token>
```

La documentación interactiva está disponible en `http://localhost:5238/scalar/v1` y el contrato OpenAPI en `http://localhost:5238/openapi/v1.json`.

## 2. Flujo funcional

1. El usuario inicia sesión con `POST /api/auth/login`.
2. El frontend guarda el token y lo adjunta a todas las peticiones de comercio.
3. El usuario selecciona un CSV llamado `commerce_DDMMYYYY.csv`.
4. `POST /api/commerce/upload` carga el archivo.
5. `POST /api/commerce/process` procesa la fecha indicada y mueve los registros inválidos a cuarentena.
6. `GET /api/commerce/quarantine` lista los registros rechazados, con filtros y paginación.

La carga y el procesamiento son operaciones separadas. Cargar un archivo no lo procesa automáticamente.

## 3. Contrato de errores

La API devuelve errores con el formato `ProblemDetails`:

```json
{
  "type": "https://tools.ietf.org/html/rfc9110#section-15.5.1",
  "title": "Solicitud inválida",
  "status": 400,
  "detail": "El archivo debe llamarse commerce_DDMMYYYY.csv con una fecha válida.",
  "instance": "/api/commerce/upload",
  "traceId": "0HN..."
}
```

Códigos relevantes:

| Código | Significado |
|---|---|
| `200` | Operación correcta |
| `400` | Datos inválidos, archivo inválido o credenciales incorrectas |
| `401` | Token ausente, vencido o inválido |
| `403` | Sin permisos (la API actual no usa roles en los endpoints) |
| `409` | El CSV ya fue cargado anteriormente |
| `415` | `Content-Type` incorrecto |
| `429` | Demasiados intentos de login |
| `500` | Error interno |

Los errores de autenticación y de rate limit también devuelven `ProblemDetails`. El campo `traceId` sirve para correlacionar con los logs del backend.

## 4. Autenticación

### 4.1 Usuario de desarrollo

| Usuario | Contraseña |
|---|---|
| `admin` | `Admin123!` |

Estas credenciales se crean con `db/06_auth.sql` y son solo para desarrollo.

### 4.2 Login

`POST /api/auth/login`

```json
{
  "username": "admin",
  "password": "Admin123!"
}
```

Respuesta `200`:

```json
{
  "accessToken": "eyJhbGciOiJIUzI1NiIs...",
  "tokenType": "Bearer",
  "expiresAt": "2026-10-21T18:30:00+00:00",
  "username": "admin",
  "role": "Admin"
}
```

Credenciales incorrectas: `400` con detalle genérico:

```json
{
  "title": "Solicitud inválida",
  "status": 400,
  "detail": "Usuario o contraseña inválidos."
}
```

El login tiene un límite de 5 intentos por minuto y por IP. Al superarlo devuelve `429` con `Retry-After: 60`.

### 4.3 Usuario actual

`GET /api/auth/me` con `Authorization: Bearer <token>`.

```json
{
  "username": "admin",
  "role": "Admin"
}
```

Se puede usar al iniciar la aplicación para comprobar que el token guardado sigue siendo válido.

### 4.4 Manejo del token

- El token expira a los 60 minutos.
- No hay endpoint de refresh ni de registro.
- Ante `401`, elimina el token y redirige al login.
- Guarda el token en `sessionStorage` o en memoria. Evita `localStorage` si quieres reducir el impacto de XSS.
- No muestres el token en logs ni en mensajes de error.

## 5. Endpoints de comercio

Todos requieren `Authorization: Bearer <token>`.

### 5.1 Cargar CSV

`POST /api/commerce/upload`

`Content-Type: multipart/form-data`

| Campo | Tipo | Obligatorio |
|---|---|---|
| `file` | archivo CSV | Sí |

No establezcas manualmente `Content-Type` en Angular cuando uses `FormData`; el navegador agrega el boundary.

Restricciones:

- Tamaño máximo: 20 MB.
- Nombre: `commerce_DDMMYYYY.csv` (por ejemplo, `commerce_01112026.csv`).
- La fecha del nombre debe ser válida.
- Todas las filas deben tener la misma fecha de proceso que indica el nombre.
- Columnas obligatorias: `pc_nomcomred`, `pc_numdoc`, `pc_processdate`.
- Se admiten delimitadores coma o punto y coma.
- `pc_nomcomred` admite hasta 200 caracteres.
- `pc_numdoc` admite hasta 50 caracteres.

Ejemplo de CSV:

```csv
pc_nomcomred,pc_numdoc,pc_processdate
Comercio Alfa,10000001,2026-11-01
Comercio Beta,10000002,2026-11-01
Comercio Gamma,10000003,2026-11-01
```

Formatos de fecha aceptados: `yyyy-MM-dd`, `dd/MM/yyyy`, `ddMMyyyy`, `yyyy/MM/dd`, `yyyy-MM-dd HH:mm:ss`, `dd/MM/yyyy HH:mm:ss`, `yyyy-MM-ddTHH:mm:ss`.

Respuesta `200`:

```json
{
  "fileName": "commerce_01112026.csv",
  "processDate": "2026-11-01",
  "rowsRead": 4,
  "rowsInserted": 4
}
```

Duplicado (`409`):

```json
{
  "title": "Conflicto",
  "status": 409,
  "detail": "El archivo ya fue cargado. No se insertaron registros duplicados.",
  "instance": "/api/commerce/upload"
}
```

La deduplicación se basa en el SHA-256 del contenido del archivo, no en el nombre. Si el mismo contenido se sube con otro nombre, también se rechaza. La comparación es atómica en la base de datos.

### 5.2 Procesar fecha

`POST /api/commerce/process`

```json
{
  "processDate": "2026-11-01"
}
```

Respuesta `200`:

```json
{
  "processDate": "2026-11-01",
  "quarantined": 0,
  "remaining": 4
}
```

Reglas del proceso:

- Los registros con nombre de comercio vacío se envían a cuarentena.
- Los registros con documento vacío o con caracteres no numéricos se envían a cuarentena.
- Los registros válidos permanecen en `commerce`.
- El proceso es idempotente por fecha: ejecutarlo de nuevo no vuelve a mover los mismos registros.
- `processDate` es obligatorio y usa formato `YYYY-MM-DD`.

### 5.3 Consultar cuarentena

`GET /api/commerce/quarantine`

| Parámetro | Tipo | Obligatorio | Descripción |
|---|---|---|---|
| `processDate` | `YYYY-MM-DD` | No | Filtra por fecha; si se omite, devuelve todas |
| `search` | texto | No | Busca en nombre de comercio o documento; máximo 100 caracteres |
| `page` | entero | No | Página desde 1; por defecto 1 |
| `pageSize` | entero | No | Entre 1 y 100; por defecto 20 |

Ejemplo:

```http
GET /api/commerce/quarantine?processDate=2026-11-02&search=Comercio&page=1&pageSize=20
```

Respuesta `200`:

```json
{
  "items": [
    {
      "id": 15,
      "merchantName": null,
      "documentNumber": "12345678",
      "processDate": "2026-11-02",
      "motivo": "El nombre del comercio (pc_nomcomred) se encuentra vacío"
    }
  ],
  "page": 1,
  "pageSize": 20,
  "total": 1
}
```

`merchantName` y `documentNumber` pueden ser `null`. `processDate` también puede ser `null` en registros históricos.

## 6. Reglas de negocio que debe reflejar la UI

- No intentes subir dos veces el mismo archivo; si ocurre, muestra el mensaje del `409`.
- Después de una carga correcta, ofrece procesar esa fecha.
- Después de procesar, muestra `quarantined` y `remaining`.
- La fecha del nombre del archivo y la fecha de las filas debe coincidir; si no, la API devuelve `400` antes de insertar.
- La carga no valida la calidad de los datos de negocio; eso ocurre en el proceso. Un archivo puede cargarse correctamente y luego tener filas en cuarentena.
- Los registros en cuarentena conservan el motivo devuelto en `motivo`.
- La búsqueda de cuarentena no distingue mayúsculas/minúsculas si la colación de SQL Server es la habitual.
- Las fechas son `DateOnly`: no apliques conversiones de zona horaria.

## 7. Implementación sugerida en Angular

### 7.1 Modelos

```ts
export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresAt: string;
  username: string;
  role: string;
}

export interface CurrentUser {
  username: string;
  role: string;
}

export interface UploadResult {
  fileName: string;
  processDate: string;
  rowsRead: number;
  rowsInserted: number;
}

export interface ProcessResult {
  processDate: string;
  quarantined: number;
  remaining: number;
}

export interface QuarantineItem {
  id: number;
  merchantName: string | null;
  documentNumber: string | null;
  processDate: string | null;
  motivo: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
}
```

### 7.2 Servicio de autenticación

```ts
import { HttpClient } from '@angular/common/http';
import { Injectable, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { CurrentUser, LoginRequest, LoginResponse } from './models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly tokenKey = 'iroute.token';
  private readonly userKey = 'iroute.user';

  readonly user = signal<CurrentUser | null>(this.readUser());

  login(request: LoginRequest) {
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/api/auth/login`, request)
      .pipe(
        tap((response) => {
          sessionStorage.setItem(this.tokenKey, response.accessToken);
          const user = { username: response.username, role: response.role };
          sessionStorage.setItem(this.userKey, JSON.stringify(user));
          this.user.set(user);
        })
      );
  }

  me() {
    return this.http.get<CurrentUser>(`${environment.apiUrl}/api/auth/me`);
  }

  logout(): void {
    sessionStorage.removeItem(this.tokenKey);
    sessionStorage.removeItem(this.userKey);
    this.user.set(null);
    void this.router.navigate(['/login']);
  }

  get token(): string | null {
    return sessionStorage.getItem(this.tokenKey);
  }

  get isAuthenticated(): boolean {
    return Boolean(this.token);
  }

  private readUser(): CurrentUser | null {
    const raw = sessionStorage.getItem(this.userKey);
    return raw ? (JSON.parse(raw) as CurrentUser) : null;
  }
}
```

### 7.3 Interceptor y guard

```ts
import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const token = auth.token;

  const authorized = token
    ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : request;

  return next(authorized).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        auth.logout();
      }

      return throwError(() => error);
    })
  );
};
```

```ts
import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  const router = inject(Router);

  return auth.isAuthenticated ? true : router.createUrlTree(['/login']);
};
```

Registro en `app.config.ts`:

```ts
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { provideRouter } from '@angular/router';
import { authInterceptor } from './core/auth.interceptor';
import { routes } from './app.routes';

export const appConfig = {
  providers: [
    provideRouter(routes),
    provideHttpClient(withInterceptors([authInterceptor]))
  ]
};
```

### 7.4 Servicio de comercio

```ts
import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { environment } from '../../environments/environment';
import { PagedResult, ProcessResult, QuarantineItem, UploadResult } from './models';

export interface QuarantineFilters {
  processDate?: string;
  search?: string;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class CommerceService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/commerce`;

  upload(file: File) {
    const form = new FormData();
    form.append('file', file);
    return this.http.post<UploadResult>(`${this.baseUrl}/upload`, form);
  }

  process(processDate: string) {
    return this.http.post<ProcessResult>(`${this.baseUrl}/process`, { processDate });
  }

  getQuarantine(filters: QuarantineFilters) {
    let params = new HttpParams()
      .set('page', filters.page)
      .set('pageSize', filters.pageSize);

    if (filters.processDate) {
      params = params.set('processDate', filters.processDate);
    }

    if (filters.search?.trim()) {
      params = params.set('search', filters.search.trim());
    }

    return this.http.get<PagedResult<QuarantineItem>>(`${this.baseUrl}/quarantine`, { params });
  }
}
```

### 7.5 Validación local del archivo

```ts
export function validateCsvFile(file: File): string | null {
  const match = /^commerce_(\d{8})\.csv$/i.exec(file.name);

  if (!match) {
    return 'El archivo debe llamarse commerce_DDMMYYYY.csv.';
  }

  const raw = match[1];
  const day = Number(raw.slice(0, 2));
  const month = Number(raw.slice(2, 4));
  const year = Number(raw.slice(4, 8));
  const date = new Date(year, month - 1, day);

  if (
    date.getFullYear() !== year ||
    date.getMonth() !== month - 1 ||
    date.getDate() !== day
  ) {
    return 'La fecha del nombre del archivo no es válida.';
  }

  if (file.size > 20 * 1024 * 1024) {
    return 'El archivo supera el límite de 20 MB.';
  }

  return null;
}
```

Esta validación es solo para dar feedback inmediato. La API vuelve a validar todo.

### 7.6 Manejo de errores en componentes

```ts
import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from './models';

export function problemMessage(error: unknown, fallback: string): string {
  if (error instanceof HttpErrorResponse) {
    const problem = error.error as ProblemDetails | null;
    return problem?.detail ?? problem?.title ?? fallback;
  }

  return fallback;
}
```

Ejemplo de uso:

```ts
this.commerce.upload(file).subscribe({
  next: (result) => {
    this.uploadResult.set(result);
    this.selectedDate.set(result.processDate);
  },
  error: (error: unknown) => {
    this.error.set(problemMessage(error, 'No se pudo cargar el archivo.'));
  }
});
```

## 8. Pantallas mínimas

| Ruta | Pantalla | Contenido |
|---|---|---|
| `/login` | Login | Usuario, contraseña, error de credenciales |
| `/upload` | Carga | Selector de archivo, validación local, resultado y botón para procesar |
| `/process` | Procesamiento | Selector de fecha, resultados `quarantined` y `remaining` |
| `/quarantine` | Cuarentena | Filtro por fecha, búsqueda, tabla paginada y motivos |

Sugerencias de UX:

- Después del login, redirige a `/upload`.
- Muestra un spinner durante la carga y el proceso.
- Deshabilita el botón de carga mientras la petición está en curso.
- Si recibes `409`, no reintentes automáticamente; informa que el archivo ya fue cargado.
- Si recibes `401`, cierra la sesión.
- Si recibes `429` en login, muestra el mensaje y espera antes de reintentar.
- En cuarentena, usa debounce de 300–500 ms para la búsqueda y reinicia `page` a `1` al cambiar filtros.
- Muestra `total` y calcula el número de páginas con `Math.ceil(total / pageSize)`.

## 9. Previsualización opcional

La API no tiene un endpoint de previsualización. Si se necesita mostrar el CSV antes de cargarlo, se puede leer el archivo en el navegador con `FileReader` y una librería como Papa Parse, o con lógica propia.

La previsualización local no reemplaza la validación del backend:

- El backend valida encabezados, fechas, longitudes y fechas por fila.
- La previsualización debe limitarse a las primeras filas para no bloquear el navegador.
- No envíes el archivo dos veces como consecuencia de una previsualización.

## 10. Pruebas manuales

Backend:

```powershell
dotnet run --project backend --launch-profile http
```

Runner de samples:

```powershell
.\samples\run-api-tests.ps1
```

El runner inicia sesión con `admin`, prueba credenciales inválidas, verifica el `401`, carga los CSV de ejemplo, comprueba el `409` al repetir una carga, procesa las fechas y consulta la cuarentena.

Base de datos:

```powershell
.\db\run.ps1
```

Para recrear el usuario de desarrollo, ejecuta `db/06_auth.sql`.

## 11. Seguridad y despliegue

- El usuario `admin` y su contraseña son de desarrollo. Cámbialos antes de desplegar.
- Configura `Jwt:Key` con al menos 32 bytes mediante variable de entorno (`Jwt__Key`) o user secrets. No uses la clave de desarrollo en producción.
- En producción, la API exige HTTPS (`RequireHttpsMetadata = true`) y redirige HTTP a HTTPS.
- El token dura 60 minutos y no se renueva automáticamente.
- El login está limitado a 5 intentos por minuto por IP.
- Las contraseñas se almacenan con PBKDF2-SHA256 y comparación en tiempo constante.
- El frontend no debe registrar tokens, contraseñas ni contenido completo de archivos en la consola.
- La API no implementa registro, recuperación de contraseña ni refresh tokens; si se necesitan, deben agregarse en el backend.
