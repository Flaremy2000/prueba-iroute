import { HttpErrorResponse } from '@angular/common/http';
import { ProblemDetails } from './models';

export function problemMessage(error: unknown, fallback: string): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallback;
  }

  if (error.status === 0) {
    return 'No se pudo conectar con la API. Verifica que el backend esté iniciado.';
  }

  const problem = error.error as ProblemDetails | null;
  const detail = problem?.detail?.trim();
  const title = problem?.title?.trim();

  if (detail) {
    return detail;
  }

  if (title) {
    return title;
  }

  if (error.status === 401) {
    return 'Tu sesión expiró. Inicia sesión nuevamente.';
  }

  if (error.status === 429) {
    return 'Demasiados intentos. Espera un minuto antes de volver a intentar.';
  }

  if (error.status >= 500) {
    return 'La API presentó un error interno. Intenta nuevamente en unos minutos.';
  }

  return fallback;
}
