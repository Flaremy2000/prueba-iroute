import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { signal } from '@angular/core';
import { AuthService } from '../../core/auth.service';
import { Shell } from './shell';

describe('Shell', () => {
  const auth = {
    user: signal({ username: 'admin', role: 'Admin' }),
    logout: vi.fn()
  };

  let component: Shell;

  beforeEach(() => {
    vi.clearAllMocks();

    TestBed.configureTestingModule({
      imports: [Shell],
      providers: [
        { provide: AuthService, useValue: auth },
        provideRouter([])
      ]
    });

    component = TestBed.createComponent(Shell).componentInstance;
  });

  it('builds the user initials', () => {
    expect(component.initials()).toBe('AD');
  });

  it('opens and closes the mobile menu', () => {
    component.mobileOpen.set(true);
    component.closeMobile();

    expect(component.mobileOpen()).toBe(false);
  });

  it('delegates logout to the auth service', () => {
    component.logout();

    expect(auth.logout).toHaveBeenCalledTimes(1);
  });
});
