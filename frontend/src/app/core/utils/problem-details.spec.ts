import { HttpErrorResponse } from '@angular/common/http';
import { problemMessage } from './problem-details';

describe('problemMessage', () => {
  it('returns the fallback for non-HTTP errors', () => {
    expect(problemMessage(new Error('fail'), 'Fallback')).toBe('Fallback');
  });

  it('reports connection failures on status 0', () => {
    const error = new HttpErrorResponse({ status: 0 });
    expect(problemMessage(error, 'Fallback')).toContain('No se pudo conectar');
  });

  it('prefers the detail message from ProblemDetails', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { title: 'Bad request', detail: 'El archivo está vacío.' }
    });
    expect(problemMessage(error, 'Fallback')).toBe('El archivo está vacío.');
  });

  it('uses the title when detail is missing', () => {
    const error = new HttpErrorResponse({
      status: 400,
      error: { title: 'Archivo duplicado' }
    });
    expect(problemMessage(error, 'Fallback')).toBe('Archivo duplicado');
  });

  it('provides a friendly message on 401', () => {
    const error = new HttpErrorResponse({ status: 401 });
    expect(problemMessage(error, 'Fallback')).toContain('expiró');
  });

  it('provides a friendly message on 429', () => {
    const error = new HttpErrorResponse({ status: 429 });
    expect(problemMessage(error, 'Fallback')).toContain('Demasiados intentos');
  });

  it('provides a friendly message on 500', () => {
    const error = new HttpErrorResponse({ status: 500 });
    expect(problemMessage(error, 'Fallback')).toContain('error interno');
  });
});
