import { isPlatformBrowser } from '@angular/common';
import { HttpClient } from '@angular/common/http';
import { Injectable, PLATFORM_ID, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { CurrentUser, LoginRequest, LoginResponse } from './models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  private readonly platformId = inject(PLATFORM_ID);
  private readonly browser = isPlatformBrowser(this.platformId);
  private readonly tokenKey = 'iroute.token';
  private readonly userKey = 'iroute.user';
  private readonly expiresAtKey = 'iroute.expiresAt';

  private readonly userState = signal<CurrentUser | null>(this.readUser());
  readonly user = this.userState.asReadonly();

  login(request: LoginRequest): Observable<LoginResponse> {
    return this.http
      .post<LoginResponse>(`${environment.apiUrl}/api/auth/login`, request)
      .pipe(tap((response) => this.saveSession(response)));
  }

  me(): Observable<CurrentUser> {
    return this.http.get<CurrentUser>(`${environment.apiUrl}/api/auth/me`);
  }

  refreshUser(): Observable<CurrentUser> {
    return this.me().pipe(
      tap((user) => {
        this.userState.set(user);
        this.storage()?.setItem(this.userKey, JSON.stringify(user));
      })
    );
  }

  logout(): void {
    this.clearSession();
    this.userState.set(null);

    if (this.browser) {
      void this.router.navigate(['/login'], { replaceUrl: true });
    }
  }

  get token(): string | null {
    return this.storage()?.getItem(this.tokenKey) ?? null;
  }

  get isAuthenticated(): boolean {
    const token = this.token;

    if (!token) {
      return false;
    }

    const expiresAt = this.readExpiry(token);

    return expiresAt === null || expiresAt > Date.now() + 5_000;
  }

  private saveSession(response: LoginResponse): void {
    const user: CurrentUser = {
      username: response.username,
      role: response.role
    };

    this.userState.set(user);

    const storage = this.storage();

    if (!storage) {
      return;
    }

    storage.setItem(this.tokenKey, response.accessToken);
    storage.setItem(this.userKey, JSON.stringify(user));

    const expiresAt = Date.parse(response.expiresAt);

    if (Number.isFinite(expiresAt)) {
      storage.setItem(this.expiresAtKey, expiresAt.toString());
    }
  }

  private clearSession(): void {
    const storage = this.storage();

    if (!storage) {
      return;
    }

    storage.removeItem(this.tokenKey);
    storage.removeItem(this.userKey);
    storage.removeItem(this.expiresAtKey);
  }

  private readUser(): CurrentUser | null {
    const raw = this.storage()?.getItem(this.userKey);

    if (!raw) {
      return null;
    }

    try {
      const user = JSON.parse(raw) as Partial<CurrentUser>;

      if (typeof user.username !== 'string' || typeof user.role !== 'string') {
        return null;
      }

      return { username: user.username, role: user.role };
    } catch {
      return null;
    }
  }

  private readExpiry(token: string): number | null {
    const stored = this.storage()?.getItem(this.expiresAtKey);

    if (stored) {
      const value = Number(stored);

      if (Number.isFinite(value)) {
        return value;
      }
    }

    const payload = token.split('.')[1];

    if (!payload) {
      return null;
    }

    try {
      const normalized = payload.replace(/-/g, '+').replace(/_/g, '/');
      const padded = normalized.padEnd(Math.ceil(normalized.length / 4) * 4, '=');
      const json = JSON.parse(atob(padded)) as { exp?: unknown };

      return typeof json.exp === 'number' ? json.exp * 1_000 : null;
    } catch {
      return null;
    }
  }

  private storage(): Storage | null {
    return this.browser ? sessionStorage : null;
  }
}
