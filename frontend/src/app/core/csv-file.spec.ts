import { formatFileSize, processDateFromFileName, validateCsvFile } from './csv-file';

function fakeFile(name: string, size: number): File {
  return { name, size } as File;
}

describe('csv-file', () => {
  describe('processDateFromFileName', () => {
    it('extracts a valid ISO date', () => {
      expect(processDateFromFileName('commerce_01112026.csv')).toBe('2026-11-01');
    });

    it('accepts the extension in any case', () => {
      expect(processDateFromFileName('COMMERCE_31122026.CSV')).toBe('2026-12-31');
    });

    it('rejects an invalid calendar date', () => {
      expect(processDateFromFileName('commerce_32132026.csv')).toBeNull();
      expect(processDateFromFileName('commerce_29022025.csv')).toBeNull();
    });

    it('rejects an unexpected name', () => {
      expect(processDateFromFileName('comercios.csv')).toBeNull();
    });
  });

  describe('validateCsvFile', () => {
    it('accepts a valid file', () => {
      expect(validateCsvFile(fakeFile('commerce_01112026.csv', 1024))).toBeNull();
    });

    it('rejects an invalid name', () => {
      expect(validateCsvFile(fakeFile('commerce.csv', 1024))).toBe(
        'El archivo debe llamarse commerce_DDMMYYYY.csv.'
      );
    });

    it('rejects an invalid date in the name', () => {
      expect(validateCsvFile(fakeFile('commerce_31132026.csv', 1024))).toBe(
        'La fecha del nombre del archivo no es válida.'
      );
    });

    it('rejects an empty file', () => {
      expect(validateCsvFile(fakeFile('commerce_01112026.csv', 0))).toBe(
        'El archivo está vacío.'
      );
    });

    it('rejects a file over 20 MB', () => {
      expect(validateCsvFile(fakeFile('commerce_01112026.csv', 20 * 1024 * 1024 + 1))).toBe(
        'El archivo supera el límite de 20 MB.'
      );
    });
  });

  describe('formatFileSize', () => {
    it('formats bytes, kilobytes and megabytes', () => {
      expect(formatFileSize(512)).toBe('512 B');
      expect(formatFileSize(1536)).toBe('1.5 KB');
      expect(formatFileSize(2 * 1024 * 1024)).toBe('2.0 MB');
    });
  });
});
