import { TestBed } from '@angular/core/testing';
import { Router, RouterStateSnapshot, UrlTree } from '@angular/router';
import { AuthService } from './auth.service';
import { authGuard, guestGuard } from './auth.guard';

describe('authGuard', () => {
  const auth = { isAuthenticated: true };
  const router = { createUrlTree: vi.fn() };

  beforeEach(() => {
    vi.clearAllMocks();
    auth.isAuthenticated = true;

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router }
      ]
    });
  });

  it('allows access when authenticated', () => {
    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as never, { url: '/upload' } as RouterStateSnapshot)
    );

    expect(result).toBe(true);
  });

  it('redirects to login with the return URL when unauthenticated', () => {
    auth.isAuthenticated = false;
    const tree = {} as UrlTree;
    router.createUrlTree.mockReturnValue(tree);

    const result = TestBed.runInInjectionContext(() =>
      authGuard({} as never, { url: '/quarantine?date=2026-11-01' } as RouterStateSnapshot)
    );

    expect(result).toBe(tree);
    expect(router.createUrlTree).toHaveBeenCalledWith(['/login'], {
      queryParams: { returnUrl: '/quarantine?date=2026-11-01' }
    });
  });

  it('allows guests into the login page', () => {
    auth.isAuthenticated = false;

    const result = TestBed.runInInjectionContext(() => guestGuard({} as never, {} as never));
    expect(result).toBe(true);
  });

  it('redirects authenticated users away from the login page', () => {
    auth.isAuthenticated = true;
    const tree = {} as UrlTree;
    router.createUrlTree.mockReturnValue(tree);

    const result = TestBed.runInInjectionContext(() => guestGuard({} as never, {} as never));
    expect(result).toBe(tree);
    expect(router.createUrlTree).toHaveBeenCalledWith(['/upload']);
  });
});
