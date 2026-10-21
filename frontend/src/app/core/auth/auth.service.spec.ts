import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { LoginResponse } from '../models';
import { AuthService } from './auth.service';

function tokenWithExpiry(expiresAt: number): string {
  const payload = btoa(JSON.stringify({ exp: Math.floor(expiresAt / 1000) }))
    .replace(/\+/g, '-')
    .replace(/\//g, '_')
    .replace(/=+$/g, '');

  return `header.${payload}.signature`;
}

describe('AuthService', () => {
  let auth: AuthService;
  let http: HttpTestingController;
  let router: Router;

  function configure(): void {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        {
          provide: Router,
          useValue: { navigate: vi.fn().mockResolvedValue(true) }
        }
      ]
    });

    auth = TestBed.inject(AuthService);
    http = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  }

  beforeEach(() => {
    sessionStorage.clear();
    TestBed.resetTestingModule();
    configure();
  });

  afterEach(() => {
    http.verify();
  });

  it('stores the token, user and expiration after login', () => {
    const response: LoginResponse = {
      accessToken: 'access-token',
      tokenType: 'Bearer',
      expiresAt: new Date(Date.now() + 3_600_000).toISOString(),
      username: 'admin',
      role: 'Admin'
    };

    auth.login({ username: 'admin', password: 'Admin123!' }).subscribe();

    const request = http.expectOne('http://localhost:5238/api/auth/login');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ username: 'admin', password: 'Admin123!' });
    request.flush(response);

    expect(auth.token).toBe('access-token');
    expect(auth.user()).toEqual({ username: 'admin', role: 'Admin' });
    expect(auth.isAuthenticated).toBe(true);
  });

  it('does not consider an expired token valid', () => {
    const response: LoginResponse = {
      accessToken: 'expired-token',
      tokenType: 'Bearer',
      expiresAt: new Date(Date.now() - 1_000).toISOString(),
      username: 'admin',
      role: 'Admin'
    };

    auth.login({ username: 'admin', password: 'Admin123!' }).subscribe();
    http.expectOne('http://localhost:5238/api/auth/login').flush(response);

    expect(auth.isAuthenticated).toBe(false);
  });

  it('reads the expiration from the JWT when no stored value exists', () => {
    sessionStorage.setItem('iroute.token', tokenWithExpiry(Date.now() + 3_600_000));
    sessionStorage.setItem(
      'iroute.user',
      JSON.stringify({ username: 'admin', role: 'Admin' })
    );

    TestBed.resetTestingModule();
    configure();

    expect(auth.isAuthenticated).toBe(true);
    expect(auth.user()).toEqual({ username: 'admin', role: 'Admin' });
  });

  it('rejects an expired JWT', () => {
    sessionStorage.setItem('iroute.token', tokenWithExpiry(Date.now() - 1_000));

    TestBed.resetTestingModule();
    configure();

    expect(auth.isAuthenticated).toBe(false);
  });

  it('ignores malformed stored user data', () => {
    sessionStorage.setItem('iroute.token', 'token');
    sessionStorage.setItem('iroute.user', '{not-json');

    TestBed.resetTestingModule();
    configure();

    expect(auth.user()).toBeNull();
  });

  it('logs out and clears the session', () => {
    sessionStorage.setItem('iroute.token', 'token');
    sessionStorage.setItem('iroute.user', JSON.stringify({ username: 'admin', role: 'Admin' }));

    TestBed.resetTestingModule();
    configure();

    auth.logout();

    expect(auth.token).toBeNull();
    expect(auth.user()).toBeNull();
    expect(sessionStorage.getItem('iroute.token')).toBeNull();
    expect(sessionStorage.getItem('iroute.user')).toBeNull();
    expect(vi.mocked(router.navigate)).toHaveBeenCalledWith(['/login'], { replaceUrl: true });
  });

  it('refreshes the current user from the API', () => {
    auth.refreshUser().subscribe();

    const request = http.expectOne('http://localhost:5238/api/auth/me');
    expect(request.request.method).toBe('GET');
    request.flush({ username: 'operador', role: 'Admin' });

    expect(auth.user()).toEqual({ username: 'operador', role: 'Admin' });
    expect(sessionStorage.getItem('iroute.user')).toContain('operador');
  });

  it('does not persist a session when login fails', () => {
    auth.login({ username: 'admin', password: 'bad' }).subscribe({
      error: () => undefined
    });

    http
      .expectOne('http://localhost:5238/api/auth/login')
      .flush({ detail: 'Usuario o contraseña inválidos.' }, { status: 400, statusText: 'Bad Request' });

    expect(auth.token).toBeNull();
    expect(auth.user()).toBeNull();
  });
});
