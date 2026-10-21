import { ChangeDetectionStrategy, Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { debounceTime, distinctUntilChanged, finalize } from 'rxjs';
import { CommerceService } from '../../../core/commerce';
import { QuarantineItem } from '../../../core/models';
import { formatIsoDate, formatNumber, isIsoDate, problemMessage } from '../../../core/utils';

@Component({
  selector: 'app-quarantine-page',
  imports: [ReactiveFormsModule],
  templateUrl: './quarantine.html',
  styleUrl: './quarantine.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class QuarantinePage implements OnInit {
  private readonly commerce = inject(CommerceService);
  private readonly route = inject(ActivatedRoute);
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly destroyRef = inject(DestroyRef);

  readonly filters = this.formBuilder.group({
    processDate: [this.initialDate()],
    search: ['', [Validators.maxLength(100)]],
    pageSize: [20]
  });

  readonly items = signal<QuarantineItem[]>([]);
  readonly total = signal(0);
  readonly page = signal(1);
  readonly pageSize = signal(20);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly totalPages = computed(() => Math.max(1, Math.ceil(this.total() / this.pageSize())));
  readonly rangeStart = computed(() =>
    this.total() === 0 ? 0 : (this.page() - 1) * this.pageSize() + 1
  );
  readonly rangeEnd = computed(() => Math.min(this.page() * this.pageSize(), this.total()));

  private lastRequest = 0;

  constructor() {
    this.filters.controls.processDate.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.page.set(1);
        this.load();
      });

    this.filters.controls.search.valueChanges
      .pipe(debounceTime(400), distinctUntilChanged(), takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        this.page.set(1);
        this.load();
      });

    this.filters.controls.pageSize.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((pageSize) => {
        this.pageSize.set(pageSize);
        this.page.set(1);
        this.load();
      });
  }

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    const request = ++this.lastRequest;

    this.loading.set(true);
    this.error.set(null);

    this.commerce
      .getQuarantine({
        processDate: this.filters.controls.processDate.value || undefined,
        search: this.filters.controls.search.value.trim() || undefined,
        page: this.page(),
        pageSize: this.pageSize()
      })
      .pipe(
        finalize(() => {
          if (request === this.lastRequest) {
            this.loading.set(false);
          }
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (result) => {
          if (request !== this.lastRequest) {
            return;
          }

          this.items.set(result.items);
          this.total.set(result.total);
          this.page.set(result.page);
          this.pageSize.set(result.pageSize);
        },
        error: (error: unknown) => {
          if (request !== this.lastRequest) {
            return;
          }

          this.items.set([]);
          this.total.set(0);
          this.error.set(problemMessage(error, 'No se pudo consultar la cuarentena.'));
        }
      });
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages() || page === this.page() || this.loading()) {
      return;
    }

    this.page.set(page);
    this.load();
  }

  clearFilters(): void {
    this.filters.reset(
      { processDate: '', search: '', pageSize: 20 },
      { emitEvent: false }
    );
    this.pageSize.set(20);
    this.page.set(1);
    this.load();
  }

  hasFilters(): boolean {
    return Boolean(
      this.filters.controls.processDate.value || this.filters.controls.search.value.trim()
    );
  }

  formatDate(value: string | null | undefined): string {
    return formatIsoDate(value);
  }

  formatCount(value: number): string {
    return formatNumber(value);
  }

  private initialDate(): string {
    const date = this.route.snapshot.queryParamMap.get('date');
    const processDate = this.route.snapshot.queryParamMap.get('processDate');
    const candidate = date ?? processDate;

    return isIsoDate(candidate) ? candidate : '';
  }
}
