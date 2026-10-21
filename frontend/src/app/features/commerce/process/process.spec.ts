import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { CommerceService } from '../../../core/commerce.service';
import { ProcessPage } from './process';

describe('ProcessPage', () => {
  const commerce = { process: vi.fn() };
  const route = {
    snapshot: {
      queryParamMap: convertToParamMap({ date: '2026-11-05' })
    }
  };

  let component: ProcessPage;

  beforeEach(() => {
    vi.clearAllMocks();

    TestBed.configureTestingModule({
      imports: [ProcessPage],
      providers: [
        { provide: CommerceService, useValue: commerce },
        provideRouter([]),
        { provide: ActivatedRoute, useValue: route }
      ]
    });

    component = TestBed.createComponent(ProcessPage).componentInstance;
  });

  it('prefills the date from the query string', () => {
    expect(component.form.controls.processDate.value).toBe('2026-11-05');
  });

  it('processes the selected date and stores the summary', () => {
    const result = { processDate: '2026-11-05', quarantined: 2, remaining: 8 };
    commerce.process.mockReturnValue(of(result));

    component.submit();

    expect(commerce.process).toHaveBeenCalledWith('2026-11-05');
    expect(component.result()).toEqual(result);
    expect(component.loading()).toBe(false);
  });

  it('shows the API error when processing fails', () => {
    commerce.process.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: { detail: 'La fecha de proceso es obligatoria.' }
          })
      )
    );

    component.submit();

    expect(component.error()).toBe('La fecha de proceso es obligatoria.');
    expect(component.result()).toBeNull();
  });

  it('does not process an empty date', () => {
    component.form.controls.processDate.setValue('');

    component.submit();

    expect(commerce.process).not.toHaveBeenCalled();
    expect(component.form.controls.processDate.touched).toBe(true);
  });

  it('resets the result and restores today', () => {
    component.form.controls.processDate.setValue('2026-11-05');
    component.reset();

    expect(component.result()).toBeNull();
    expect(component.error()).toBeNull();
    expect(component.form.controls.processDate.value).toMatch(/^\d{4}-\d{2}-\d{2}$/);
  });

  it('formats the result date', () => {
    expect(component.formatDate('2026-11-05')).toContain('2026');
  });
});
