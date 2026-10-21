import { CsvPreview, CsvPreviewRow } from '../models';

const CSV_FILE_PATTERN = /^commerce_(\d{2})(\d{2})(\d{4})\.csv$/i;
const MAX_FILE_SIZE = 20 * 1024 * 1024;
const PREVIEW_CHUNK_SIZE = 64 * 1024; // 64 KB

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

export async function readCsvPreview(file: File, maxRows = 5): Promise<CsvPreview | null> {
  try {
    const chunk = file.slice(0, PREVIEW_CHUNK_SIZE);
    const text = await chunk.text();
    return parseCsvPreviewText(text, maxRows);
  } catch {
    return null;
  }
}

export function parseCsvPreviewText(text: string, maxRows = 5): CsvPreview | null {
  const lines = text
    .split(/\r?\n/)
    .map((line) => line.trim())
    .filter((line) => line.length > 0);

  if (lines.length < 2) {
    return null;
  }

  const headerLine = lines[0];
  const delimiter = headerLine.includes(';') && !headerLine.includes(',') ? ';' : ',';
  const headers = headerLine
    .split(delimiter)
    .map((header) => header.trim().replace(/^["']|["']$/g, '').toLowerCase());

  const nameIndex = headers.indexOf('pc_nomcomred');
  const docIndex = headers.indexOf('pc_numdoc');
  const dateIndex = headers.indexOf('pc_processdate');

  const rows: CsvPreviewRow[] = [];
  const sampleLines = lines.slice(1, maxRows + 1);

  for (const line of sampleLines) {
    const columns = line
      .split(delimiter)
      .map((col) => col.trim().replace(/^["']|["']$/g, ''));

    const merchantName =
      nameIndex >= 0 && nameIndex < columns.length ? columns[nameIndex] : (columns[0] ?? '');
    const documentNumber =
      docIndex >= 0 && docIndex < columns.length ? columns[docIndex] : (columns[1] ?? '');
    const processDate =
      dateIndex >= 0 && dateIndex < columns.length ? columns[dateIndex] : (columns[2] ?? '');

    rows.push({
      merchantName,
      documentNumber,
      processDate
    });
  }

  return {
    headers: ['pc_nomcomred', 'pc_numdoc', 'pc_processdate'],
    rows,
    totalSampled: rows.length
  };
}
