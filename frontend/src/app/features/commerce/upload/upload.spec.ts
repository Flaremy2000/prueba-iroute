import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { CommerceService } from '../../../core/commerce';
import { UploadPage } from './upload';

describe('UploadPage', () => {
  const commerce = { upload: vi.fn() };

  let component: UploadPage;

  beforeEach(() => {
    vi.clearAllMocks();

    TestBed.configureTestingModule({
      imports: [UploadPage],
      providers: [
        { provide: CommerceService, useValue: commerce },
        provideRouter([])
      ]
    });

    component = TestBed.createComponent(UploadPage).componentInstance;
  });

  it('selects a valid CSV and extracts its process date', () => {
    const file = new File(['pc_nomcomred\n'], 'commerce_01112026.csv', { type: 'text/csv' });

    component.selectFile(file);

    expect(component.selectedFile()).toBe(file);
    expect(component.fileDate()).toBe('2026-11-01');
    expect(component.localError()).toBeNull();
  });

  it('rejects an invalid CSV name before calling the API', () => {
    component.selectFile(new File(['data'], 'comercios.csv'));

    expect(component.selectedFile()).toBeNull();
    expect(component.localError()).toBe('El archivo debe llamarse commerce_DDMMYYYY.csv.');
    expect(commerce.upload).not.toHaveBeenCalled();
  });

  it('clears the selected file and preview', () => {
    component.selectFile(new File(['data'], 'commerce_01112026.csv'));
    component.removeFile();

    expect(component.selectedFile()).toBeNull();
    expect(component.fileDate()).toBeNull();
    expect(component.preview()).toBeNull();
    expect(component.result()).toBeNull();
  });

  it('uploads a valid file and stores the result', () => {
    const result = {
      fileName: 'commerce_01112026.csv',
      processDate: '2026-11-01',
      rowsRead: 3,
      rowsInserted: 3
    };
    commerce.upload.mockReturnValue(of(result));

    component.selectFile(new File(['data'], 'commerce_01112026.csv'));
    component.upload();

    expect(commerce.upload).toHaveBeenCalledTimes(1);
    expect(component.result()).toEqual(result);
    expect(component.loading()).toBe(false);
  });

  it('shows the duplicate message from a 409 response', () => {
    commerce.upload.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 409,
            error: { detail: 'El archivo ya fue cargado. No se insertaron registros duplicados.' }
          })
      )
    );

    component.selectFile(new File(['data'], 'commerce_01112026.csv'));
    component.upload();

    expect(component.error()).toBe(
      'El archivo ya fue cargado. No se insertaron registros duplicados.'
    );
    expect(component.result()).toBeNull();
  });

  it('does nothing when there is no selected file', () => {
    component.upload();

    expect(commerce.upload).not.toHaveBeenCalled();
  });

  it('formats the selected file size and date', () => {
    component.selectFile(new File(['data'], 'commerce_01112026.csv'));

    expect(component.fileSize()).not.toBe('');
    expect(component.formatDate(component.fileDate())).toContain('2026');
  });

  it('generates a preview when a valid file is selected', async () => {
    const csvContent = `pc_nomcomred,pc_numdoc,pc_processdate\nComercio Alfa,1001,2026-11-01`;
    const file = new File([csvContent], 'commerce_01112026.csv', { type: 'text/csv' });

    component.selectFile(file);

    await new Promise((resolve) => setTimeout(resolve, 50));

    expect(component.preview()).not.toBeNull();
    expect(component.preview()?.rows.length).toBe(1);
    expect(component.preview()?.rows[0].merchantName).toBe('Comercio Alfa');
  });
});
