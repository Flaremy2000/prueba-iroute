import { ChangeDetectionStrategy, Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { CommerceService } from '../../../core/commerce.service';
import { formatIsoDate, isIsoDate, todayIso } from '../../../core/date';
import { ProcessResult } from '../../../core/models';
import { problemMessage } from '../../../core/problem-details';

@Component({
  selector: 'app-process-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './process.html',
  styleUrl: './process.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class ProcessPage {
  private readonly commerce = inject(CommerceService);
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly form = this.formBuilder.group({
    processDate: [this.initialDate(), Validators.required]
  });

  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly result = signal<ProcessResult | null>(null);

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.error.set(null);
    this.result.set(null);

    this.commerce
      .process(this.form.controls.processDate.value)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (result) => {
          this.result.set(result);
        },
        error: (error: unknown) => {
          this.error.set(problemMessage(error, 'No se pudo procesar la fecha.'));
        }
      });
  }

  reset(): void {
    this.result.set(null);
    this.error.set(null);
    this.form.reset({ processDate: todayIso() });
  }

  formatDate(value: string | null | undefined): string {
    return formatIsoDate(value);
  }

  private initialDate(): string {
    const date = this.route.snapshot.queryParamMap.get('date');
    return isIsoDate(date) ? date : todayIso();
  }
}
