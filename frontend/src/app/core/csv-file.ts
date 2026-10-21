const CSV_FILE_PATTERN = /^commerce_(\d{2})(\d{2})(\d{4})\.csv$/i;
const MAX_FILE_SIZE = 20 * 1024 * 1024;

export function processDateFromFileName(fileName: string): string | null {
  const match = CSV_FILE_PATTERN.exec(fileName.trim());

  if (!match) {
    return null;
  }

  const day = Number(match[1]);
  const month = Number(match[2]);
  const year = Number(match[3]);
  const date = new Date(Date.UTC(year, month - 1, day));

  if (
    date.getUTCFullYear() !== year ||
    date.getUTCMonth() !== month - 1 ||
    date.getUTCDate() !== day
  ) {
    return null;
  }

  return `${year.toString().padStart(4, '0')}-${month
    .toString()
    .padStart(2, '0')}-${day.toString().padStart(2, '0')}`;
}

export function validateCsvFile(file: File): string | null {
  if (!CSV_FILE_PATTERN.test(file.name.trim())) {
    return 'El archivo debe llamarse commerce_DDMMYYYY.csv.';
  }

  if (processDateFromFileName(file.name) === null) {
    return 'La fecha del nombre del archivo no es válida.';
  }

  if (file.size === 0) {
    return 'El archivo está vacío.';
  }

  if (file.size > MAX_FILE_SIZE) {
    return 'El archivo supera el límite de 20 MB.';
  }

  return null;
}

export function formatFileSize(bytes: number): string {
  if (bytes < 1024) {
    return `${bytes} B`;
  }

  if (bytes < 1024 * 1024) {
    return `${(bytes / 1024).toFixed(1)} KB`;
  }

  return `${(bytes / (1024 * 1024)).toFixed(1)} MB`;
}
