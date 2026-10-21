import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';
import { of } from 'rxjs';
import { CommerceService } from '../../../core/commerce';
import { PagedResult, QuarantineItem } from '../../../core/models';
import { QuarantinePage } from './quarantine';

const emptyPage: PagedResult<QuarantineItem> = {
  items: [],
  page: 1,
  pageSize: 20,
  total: 0
};

describe('QuarantinePage', () => {
  const commerce = { getQuarantine: vi.fn() };
  const route = {
    snapshot: {
      queryParamMap: convertToParamMap({ date: '2026-11-02' })
    }
  };

  let component: QuarantinePage;

  beforeEach(() => {
    vi.clearAllMocks();
    commerce.getQuarantine.mockReturnValue(of(emptyPage));

    TestBed.configureTestingModule({
      imports: [QuarantinePage],
      providers: [
        { provide: CommerceService, useValue: commerce },
        { provide: ActivatedRoute, useValue: route }
      ]
    });

    component = TestBed.createComponent(QuarantinePage).componentInstance;
  });

  it('loads the date from the query string', () => {
    component.ngOnInit();

    expect(component.filters.controls.processDate.value).toBe('2026-11-02');
    expect(commerce.getQuarantine).toHaveBeenCalledWith({
      processDate: '2026-11-02',
      search: undefined,
      page: 1,
      pageSize: 20
    });
  });

  it('stores the page result and exposes pagination values', () => {
    const item: QuarantineItem = {
      id: 1,
      merchantName: null,
      documentNumber: '123',
      processDate: '2026-11-02',
      motivo: 'Nombre vacío'
    };
    commerce.getQuarantine.mockReturnValue(
      of({ items: [item], page: 1, pageSize: 20, total: 25 })
    );

    component.ngOnInit();

    expect(component.items()).toEqual([item]);
    expect(component.total()).toBe(25);
    expect(component.totalPages()).toBe(2);
    expect(component.rangeStart()).toBe(1);
    expect(component.rangeEnd()).toBe(20);
  });

  it('resets the page and loads when the date changes', () => {
    component.ngOnInit();
    commerce.getQuarantine.mockClear();
    component.page.set(3);

    component.filters.controls.processDate.setValue('2026-11-03');

    expect(component.page()).toBe(1);
    expect(commerce.getQuarantine).toHaveBeenCalledWith(
      expect.objectContaining({ processDate: '2026-11-03', page: 1 })
    );
  });

  it('debounces the search and sends the trimmed term', async () => {
    component.ngOnInit();
    commerce.getQuarantine.mockClear();

    component.filters.controls.search.setValue('  Comercio  ');
    await new Promise((resolve) => setTimeout(resolve, 450));

    expect(commerce.getQuarantine).toHaveBeenCalledWith(
      expect.objectContaining({ search: 'Comercio', page: 1 })
    );
  });

  it('changes the page size and resets to page one', () => {
    component.ngOnInit();
    commerce.getQuarantine.mockClear();
    commerce.getQuarantine.mockReturnValue(
      of({ items: [], page: 1, pageSize: 50, total: 0 })
    );
    component.page.set(2);

    component.filters.controls.pageSize.setValue(50);

    expect(component.pageSize()).toBe(50);
    expect(component.page()).toBe(1);
    expect(commerce.getQuarantine).toHaveBeenCalledWith(
      expect.objectContaining({ pageSize: 50, page: 1 })
    );
  });

  it('navigates between pages within the available range', () => {
    commerce.getQuarantine.mockReturnValue(
      of({ items: [], page: 1, pageSize: 20, total: 40 })
    );
    component.ngOnInit();
    commerce.getQuarantine.mockClear();
    commerce.getQuarantine.mockReturnValue(
      of({ items: [], page: 2, pageSize: 20, total: 40 })
    );

    component.goToPage(2);
    expect(component.page()).toBe(2);
    expect(commerce.getQuarantine).toHaveBeenCalledWith(
      expect.objectContaining({ page: 2 })
    );

    commerce.getQuarantine.mockClear();
    component.goToPage(3);
    expect(component.page()).toBe(2);
    expect(commerce.getQuarantine).not.toHaveBeenCalled();
  });

  it('clears filters and returns to the first page', () => {
    component.ngOnInit();
    component.filters.controls.search.setValue('Comercio', { emitEvent: false });
    component.filters.controls.processDate.setValue('2026-11-02', { emitEvent: false });
    component.page.set(3);
    commerce.getQuarantine.mockClear();

    component.clearFilters();

    expect(component.filters.controls.search.value).toBe('');
    expect(component.filters.controls.processDate.value).toBe('');
    expect(component.page()).toBe(1);
    expect(commerce.getQuarantine).toHaveBeenCalledWith({
      processDate: undefined,
      search: undefined,
      page: 1,
      pageSize: 20
    });
  });

  it('reports whether filters are active', () => {
    expect(component.hasFilters()).toBe(true);

    component.clearFilters();
    expect(component.hasFilters()).toBe(false);
  });

  it('formats dates and counts', () => {
    expect(component.formatDate('2026-11-02')).toContain('2026');
    expect(component.formatDate(null)).toBe('—');
    expect(component.formatCount(1200)).toBe('1.200');
  });
});
