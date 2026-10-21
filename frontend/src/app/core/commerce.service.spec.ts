import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { CommerceService } from './commerce.service';

describe('CommerceService', () => {
  let commerce: CommerceService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    commerce = TestBed.inject(CommerceService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    http.verify();
  });

  it('uploads the CSV as multipart form data', () => {
    const file = new File(['pc_nomcomred\n'], 'commerce_01112026.csv', {
      type: 'text/csv'
    });
    const result = {
      fileName: 'commerce_01112026.csv',
      processDate: '2026-11-01',
      rowsRead: 1,
      rowsInserted: 1
    };

    commerce.upload(file).subscribe((response) => {
      expect(response).toEqual(result);
    });

    const request = http.expectOne('http://localhost:5238/api/commerce/upload');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBeInstanceOf(FormData);
    const uploaded = (request.request.body as FormData).get('file') as File;
    expect(uploaded.name).toBe(file.name);
    expect(uploaded.size).toBe(file.size);
    expect(uploaded.type).toBe(file.type);
    request.flush(result);
  });

  it('sends the date to process', () => {
    commerce.process('2026-11-01').subscribe();

    const request = http.expectOne('http://localhost:5238/api/commerce/process');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ processDate: '2026-11-01' });
    request.flush({ processDate: '2026-11-01', quarantined: 1, remaining: 2 });
  });

  it('builds quarantine filters and pagination', () => {
    commerce
      .getQuarantine({
        processDate: '2026-11-02',
        search: '  Comercio  ',
        page: 2,
        pageSize: 50
      })
      .subscribe();

    const request = http.expectOne(
      (candidate) => candidate.url === 'http://localhost:5238/api/commerce/quarantine'
    );

    expect(request.request.method).toBe('GET');
    expect(request.request.params.get('processDate')).toBe('2026-11-02');
    expect(request.request.params.get('search')).toBe('Comercio');
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('50');
    request.flush({ items: [], page: 2, pageSize: 50, total: 0 });
  });

  it('omits empty quarantine filters', () => {
    commerce.getQuarantine({ page: 1, pageSize: 20 }).subscribe();

    const request = http.expectOne(
      (candidate) => candidate.url === 'http://localhost:5238/api/commerce/quarantine'
    );

    expect(request.request.params.has('processDate')).toBe(false);
    expect(request.request.params.has('search')).toBe(false);
    request.flush({ items: [], page: 1, pageSize: 20, total: 0 });
  });
});
