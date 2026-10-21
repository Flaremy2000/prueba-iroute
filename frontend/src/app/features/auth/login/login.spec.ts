import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { of, throwError } from 'rxjs';
import { AuthService } from '../../../core/auth';
import { LoginPage } from './login';

describe('LoginPage', () => {
  const auth = { login: vi.fn() };
  const router = { navigateByUrl: vi.fn().mockResolvedValue(true) };
  const route = {
    snapshot: {
      queryParamMap: convertToParamMap({ returnUrl: '/quarantine' })
    }
  };

  let component: LoginPage;

  beforeEach(() => {
    vi.clearAllMocks();

    TestBed.configureTestingModule({
      imports: [LoginPage],
      providers: [
        { provide: AuthService, useValue: auth },
        { provide: Router, useValue: router },
        { provide: ActivatedRoute, useValue: route }
      ]
    });

    component = TestBed.createComponent(LoginPage).componentInstance;
  });

  it('sends trimmed credentials and redirects to the return URL', () => {
    auth.login.mockReturnValue(
      of({
        accessToken: 'token',
        tokenType: 'Bearer',
        expiresAt: '2026-11-01T00:00:00Z',
        username: 'admin',
        role: 'Admin'
      })
    );

    component.form.setValue({ username: '  admin  ', password: 'Admin123!' });
    component.submit();

    expect(auth.login).toHaveBeenCalledWith({ username: 'admin', password: 'Admin123!' });
    expect(router.navigateByUrl).toHaveBeenCalledWith('/quarantine');
    expect(component.loading()).toBe(false);
  });

  it('shows the API detail when login fails', () => {
    auth.login.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: { detail: 'Usuario o contraseña inválidos.' }
          })
      )
    );

    component.form.setValue({ username: 'admin', password: 'bad' });
    component.submit();

    expect(component.error()).toBe('Usuario o contraseña inválidos.');
    expect(router.navigateByUrl).not.toHaveBeenCalled();
  });

  it('does not call the API when the form is invalid', () => {
    component.submit();

    expect(auth.login).not.toHaveBeenCalled();
    expect(component.form.controls.username.touched).toBe(true);
  });

  it('toggles password visibility', () => {
    expect(component.showPassword()).toBe(false);
    component.showPassword.set(!component.showPassword());
    expect(component.showPassword()).toBe(true);
  });
});
