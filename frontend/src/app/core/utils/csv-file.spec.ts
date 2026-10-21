import {
  formatFileSize,
  parseCsvPreviewText,
  processDateFromFileName,
  readCsvPreview,
  validateCsvFile
} from './csv-file';

describe('csv-file utils', () => {
  it('extracts ISO dates from valid file names', () => {
    expect(processDateFromFileName('commerce_01112026.csv')).toBe('2026-11-01');
    expect(processDateFromFileName('COMMERCE_15022026.CSV')).toBe('2026-02-15');
  });

  it('rejects invalid dates encoded in the name', () => {
    expect(processDateFromFileName('commerce_31022026.csv')).toBeNull();
    expect(processDateFromFileName('commerce_00000000.csv')).toBeNull();
    expect(processDateFromFileName('random_file.csv')).toBeNull();
  });

  it('validates file pattern, size and date', () => {
    const valid = new File(['a,b,c'], 'commerce_01112026.csv', { type: 'text/csv' });
    expect(validateCsvFile(valid)).toBeNull();

    const badName = new File(['a,b,c'], 'archivo.csv', { type: 'text/csv' });
    expect(validateCsvFile(badName)).toContain('commerce_DDMMYYYY.csv');

    const empty = new File([], 'commerce_01112026.csv', { type: 'text/csv' });
    expect(validateCsvFile(empty)).toContain('vacío');

    const bigFile = {
      name: 'commerce_01112026.csv',
      size: 25 * 1024 * 1024
    } as File;
    expect(validateCsvFile(bigFile)).toContain('20 MB');
  });

  it('formats byte lengths human-readably', () => {
    expect(formatFileSize(512)).toBe('512 B');
    expect(formatFileSize(2048)).toBe('2.0 KB');
    expect(formatFileSize(3 * 1024 * 1024)).toBe('3.0 MB');
  });

  describe('CSV preview parsing', () => {
    it('parses comma-separated rows with standard headers', () => {
      const csv = `pc_nomcomred,pc_numdoc,pc_processdate\nComercio Uno,10001,2026-11-01\nComercio Dos,10002,2026-11-01`;
      const preview = parseCsvPreviewText(csv, 5);

      expect(preview).not.toBeNull();
      expect(preview?.rows.length).toBe(2);
      expect(preview?.rows[0]).toEqual({
        merchantName: 'Comercio Uno',
        documentNumber: '10001',
        processDate: '2026-11-01'
      });
    });

    it('parses semicolon-separated rows and strips quotes', () => {
      const csv = `"pc_nomcomred";"pc_numdoc";"pc_processdate"\n"Comercio Especial";"99999";"2026-11-02"`;
      const preview = parseCsvPreviewText(csv, 5);

      expect(preview).not.toBeNull();
      expect(preview?.rows[0].merchantName).toBe('Comercio Especial');
      expect(preview?.rows[0].documentNumber).toBe('99999');
    });

    it('limits rows to maxRows parameter', () => {
      const csv = `pc_nomcomred,pc_numdoc,pc_processdate\n` +
        `A,1,2026-11-01\n` +
        `B,2,2026-11-01\n` +
        `C,3,2026-11-01\n` +
        `D,4,2026-11-01\n` +
        `E,5,2026-11-01\n` +
        `F,6,2026-11-01`;

      const preview = parseCsvPreviewText(csv, 3);
      expect(preview?.rows.length).toBe(3);
    });

    it('returns null if text has fewer than 2 lines', () => {
      expect(parseCsvPreviewText('')).toBeNull();
      expect(parseCsvPreviewText('header_only')).toBeNull();
    });

    it('reads preview from a File instance', async () => {
      const content = `pc_nomcomred,pc_numdoc,pc_processdate\nTest Comercio,12345,2026-11-01`;
      const file = new File([content], 'commerce_01112026.csv', { type: 'text/csv' });

      const preview = await readCsvPreview(file, 5);
      expect(preview).not.toBeNull();
      expect(preview?.rows[0].merchantName).toBe('Test Comercio');
    });
  });
});
