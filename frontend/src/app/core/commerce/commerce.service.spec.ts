import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ProcessResult, UploadResult } from '../models';
import { CommerceService } from './commerce.service';

describe('CommerceService', () => {
  let service: CommerceService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(CommerceService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('posts a FormData with the file to upload', () => {
    const file = new File(['content'], 'commerce_01112026.csv', { type: 'text/csv' });
    const response: UploadResult = {
      fileName: 'commerce_01112026.csv',
      processDate: '2026-11-01',
      rowsRead: 2,
      rowsInserted: 2
    };

    service.upload(file).subscribe();

    const request = http.expectOne('http://localhost:5238/api/commerce/upload');
    expect(request.request.method).toBe('POST');
    expect(request.request.body instanceof FormData).toBe(true);
    request.flush(response);
  });

  it('posts the process date payload', () => {
    const response: ProcessResult = {
      processDate: '2026-11-01',
      quarantined: 1,
      remaining: 3
    };

    service.process('2026-11-01').subscribe();

    const request = http.expectOne('http://localhost:5238/api/commerce/process');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ processDate: '2026-11-01' });
    request.flush(response);
  });

  it('queries quarantine with all filters present', () => {
    service
      .getQuarantine({
        processDate: '2026-11-01',
        search: 'Comercio',
        page: 2,
        pageSize: 50
      })
      .subscribe();

    const request = http.expectOne(
      'http://localhost:5238/api/commerce/quarantine?page=2&pageSize=50&processDate=2026-11-01&search=Comercio'
    );
    expect(request.request.method).toBe('GET');
    request.flush({ items: [], page: 2, pageSize: 50, total: 0 });
  });

  it('omits optional filters when undefined or empty', () => {
    service.getQuarantine({ page: 1, pageSize: 20, search: '   ' }).subscribe();

    const request = http.expectOne(
      'http://localhost:5238/api/commerce/quarantine?page=1&pageSize=20'
    );
    expect(request.request.method).toBe('GET');
    request.flush({ items: [], page: 1, pageSize: 20, total: 0 });
  });
});
