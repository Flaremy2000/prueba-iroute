import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthService } from './auth.service';
import { authInterceptor } from './auth.interceptor';

describe('authInterceptor', () => {
  const auth = {
    token: 'access-token' as string | null,
    logout: vi.fn()
  };

  let client: HttpClient;
  let http: HttpTestingController;

  beforeEach(() => {
    vi.clearAllMocks();
    auth.token = 'access-token';

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthService, useValue: auth }
      ]
    });

    client = TestBed.inject(HttpClient);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('adds the bearer token to API requests', () => {
    client.get('http://localhost:5238/api/commerce/quarantine').subscribe();

    const request = http.expectOne('http://localhost:5238/api/commerce/quarantine');
    expect(request.request.headers.get('Authorization')).toBe('Bearer access-token');
    request.flush({});
  });

  it('does not add the token to external requests', () => {
    client.get('https://example.test/data').subscribe();

    const request = http.expectOne('https://example.test/data');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });

  it('logs out when the API returns 401', () => {
    client
      .get('http://localhost:5238/api/commerce/quarantine')
      .subscribe({ error: () => undefined });

    http
      .expectOne('http://localhost:5238/api/commerce/quarantine')
      .flush({ detail: 'No autorizado' }, { status: 401, statusText: 'Unauthorized' });

    expect(auth.logout).toHaveBeenCalledTimes(1);
  });

  it('does not attach a token when the session is empty', () => {
    auth.token = null;
    client.get('http://localhost:5238/api/commerce/quarantine').subscribe();

    const request = http.expectOne('http://localhost:5238/api/commerce/quarantine');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });

  it('does not attach the bearer token to the login request', () => {
    auth.token = 'existing-token';
    client.post('http://localhost:5238/api/auth/login', {}).subscribe();

    const request = http.expectOne('http://localhost:5238/api/auth/login');
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });
});
