import { TestBed } from '@angular/core/testing';
import { ActivatedRouteSnapshot, Router, RouterStateSnapshot, UrlTree, provideRouter } from '@angular/router';
import { authGuard, guestGuard } from './auth.guard';
import { AuthService } from './auth.service';

describe('auth guards', () => {
  function configure(isAuthenticated: boolean): void {
    TestBed.configureTestingModule({
      providers: [
        provideRouter([]),
        {
          provide: AuthService,
          useValue: { isAuthenticated }
        }
      ]
    });
  }

  const route = {} as ActivatedRouteSnapshot;
  const state = { url: '/upload' } as RouterStateSnapshot;

  it('allows an authenticated user', () => {
    configure(true);

    expect(TestBed.runInInjectionContext(() => authGuard(route, state))).toBe(true);
  });

  it('redirects an anonymous user to login with the return URL', () => {
    configure(false);

    const result = TestBed.runInInjectionContext(() => authGuard(route, state));
    const router = TestBed.inject(Router);

    expect(result).toBeInstanceOf(UrlTree);
    expect(router.serializeUrl(result as UrlTree)).toBe('/login?returnUrl=%2Fupload');
  });

  it('redirects an authenticated user away from login', () => {
    configure(true);

    const result = TestBed.runInInjectionContext(() => guestGuard(route, state));
    const router = TestBed.inject(Router);

    expect(result).toBeInstanceOf(UrlTree);
    expect(router.serializeUrl(result as UrlTree)).toBe('/upload');
  });

  it('allows an anonymous user to see login', () => {
    configure(false);

    expect(TestBed.runInInjectionContext(() => guestGuard(route, state))).toBe(true);
  });
});
