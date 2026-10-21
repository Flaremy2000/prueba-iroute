import { HttpErrorResponse } from '@angular/common/http';
import { problemMessage } from './problem-details';

describe('problemMessage', () => {
  it('returns the API detail', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { detail: 'Usuario o contraseña inválidos.' }
    });

    expect(problemMessage(error, 'fallback')).toBe('Usuario o contraseña inválidos.');
  });

  it('falls back to the API title', () => {
    const error = new HttpErrorResponse({
      status: 409,
      error: { title: 'Conflicto' }
    });

    expect(problemMessage(error, 'fallback')).toBe('Conflicto');
  });

  it('explains a connection failure', () => {
    expect(problemMessage(new HttpErrorResponse({ status: 0 }), 'fallback')).toContain(
      'No se pudo conectar con la API'
    );
  });

  it('explains an expired session without an API body', () => {
    expect(problemMessage(new HttpErrorResponse({ status: 401 }), 'fallback')).toContain(
      'sesión expiró'
    );
  });

  it('explains a rate limit without an API body', () => {
    expect(problemMessage(new HttpErrorResponse({ status: 429 }), 'fallback')).toContain(
      'Demasiados intentos'
    );
  });

  it('explains a server error', () => {
    expect(problemMessage(new HttpErrorResponse({ status: 500 }), 'fallback')).toContain(
      'error interno'
    );
  });

  it('returns the fallback for unknown errors', () => {
    expect(problemMessage(new Error('boom'), 'fallback')).toBe('fallback');
  });
});
