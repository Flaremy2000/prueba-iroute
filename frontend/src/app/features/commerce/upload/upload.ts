import { ChangeDetectionStrategy, Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { CommerceService } from '../../../core/commerce';
import { CsvPreview, UploadResult } from '../../../core/models';
import {
  formatFileSize,
  formatIsoDate,
  problemMessage,
  processDateFromFileName,
  readCsvPreview,
  validateCsvFile
} from '../../../core/utils';

@Component({
  selector: 'app-upload-page',
  imports: [RouterLink],
  templateUrl: './upload.html',
  styleUrl: './upload.css',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class UploadPage {
  private readonly commerce = inject(CommerceService);
  private readonly destroyRef = inject(DestroyRef);

  readonly selectedFile = signal<File | null>(null);
  readonly fileDate = signal<string | null>(null);
  readonly preview = signal<CsvPreview | null>(null);
  readonly previewLoading = signal(false);
  readonly dragging = signal(false);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);
  readonly localError = signal<string | null>(null);
  readonly result = signal<UploadResult | null>(null);

  readonly fileSize = computed(() => {
    const file = this.selectedFile();
    return file ? formatFileSize(file.size) : '';
  });

  onFileSelected(event: Event): void {
    const input = event.target as HTMLInputElement;
    const file = input.files?.[0];

    if (file) {
      this.selectFile(file);
    }

    input.value = '';
  }

  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(true);
  }

  onDragLeave(): void {
    this.dragging.set(false);
  }

  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.dragging.set(false);

    const file = event.dataTransfer?.files?.[0];

    if (file) {
      this.selectFile(file);
    }
  }

  selectFile(file: File): void {
    this.error.set(null);
    this.result.set(null);

    const validationError = validateCsvFile(file);

    if (validationError) {
      this.selectedFile.set(null);
      this.fileDate.set(null);
      this.preview.set(null);
      this.localError.set(validationError);
      return;
    }

    this.localError.set(null);
    this.selectedFile.set(file);
    this.fileDate.set(processDateFromFileName(file.name));
    void this.loadPreview(file);
  }

  upload(): void {
    const file = this.selectedFile();

    if (!file || this.loading()) {
      return;
    }

    this.loading.set(true);
    this.error.set(null);

    this.commerce
      .upload(file)
      .pipe(
        finalize(() => this.loading.set(false)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (result) => {
          this.result.set(result);
          this.fileDate.set(result.processDate);
        },
        error: (error: unknown) => {
          this.error.set(problemMessage(error, 'No se pudo cargar el archivo.'));
        }
      });
  }

  removeFile(): void {
    this.selectedFile.set(null);
    this.fileDate.set(null);
    this.preview.set(null);
    this.localError.set(null);
    this.error.set(null);
    this.result.set(null);
  }

  formatDate(value: string | null | undefined): string {
    return formatIsoDate(value);
  }

  private async loadPreview(file: File): Promise<void> {
    this.previewLoading.set(true);
    try {
      const preview = await readCsvPreview(file, 5);
      this.preview.set(preview);
    } catch {
      this.preview.set(null);
    } finally {
      this.previewLoading.set(false);
    }
  }
}
