import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PagedResult, ProcessResult, QuarantineItem, UploadResult } from '../models';

export interface QuarantineFilters {
  processDate?: string;
  search?: string;
  page: number;
  pageSize: number;
}

@Injectable({ providedIn: 'root' })
export class CommerceService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiUrl}/api/commerce`;

  upload(file: File): Observable<UploadResult> {
    const form = new FormData();
    form.append('file', file, file.name);

    return this.http.post<UploadResult>(`${this.baseUrl}/upload`, form);
  }

  process(processDate: string): Observable<ProcessResult> {
    return this.http.post<ProcessResult>(`${this.baseUrl}/process`, { processDate });
  }

  getQuarantine(filters: QuarantineFilters): Observable<PagedResult<QuarantineItem>> {
    let params = new HttpParams()
      .set('page', filters.page)
      .set('pageSize', filters.pageSize);

    if (filters.processDate) {
      params = params.set('processDate', filters.processDate);
    }

    if (filters.search?.trim()) {
      params = params.set('search', filters.search.trim());
    }

    return this.http.get<PagedResult<QuarantineItem>>(`${this.baseUrl}/quarantine`, { params });
  }
}
