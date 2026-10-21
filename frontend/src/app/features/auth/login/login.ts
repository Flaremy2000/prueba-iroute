import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthService } from '../../../core/auth.service';
import { problemMessage } from '../../../core/problem-details';

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule],
  templateUrl: './login.html',
  styleUrl: './login.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class LoginPage {
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly form = this.formBuilder.group({
    username: ['', [Validators.required, Validators.maxLength(100)]],
    password: ['', [Validators.required, Validators.maxLength(200)]]
  });

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly showPassword = signal(false);

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const username = this.form.controls.username.value.trim();

    if (!username) {
      this.form.controls.username.setErrors({ required: true });
      this.form.controls.username.markAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.auth
      .login({
        username,
        password: this.form.controls.password.value
      })
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          void this.router.navigateByUrl(this.targetUrl());
        },
        error: (error: unknown) => {
          this.error.set(problemMessage(error, 'No se pudo iniciar sesión.'));
        }
      });
  }

  private targetUrl(): string {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');

    if (
      returnUrl &&
      returnUrl.startsWith('/') &&
      !returnUrl.startsWith('//') &&
      !returnUrl.startsWith('/login')
    ) {
      return returnUrl;
    }

    return '/upload';
  }
}
