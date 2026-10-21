export interface LoginRequest {
  username: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
  tokenType: string;
  expiresAt: string;
  username: string;
  role: string;
}

export interface CurrentUser {
  username: string;
  role: string;
}

export interface UploadResult {
  fileName: string;
  processDate: string;
  rowsRead: number;
  rowsInserted: number;
}

export interface ProcessResult {
  processDate: string;
  quarantined: number;
  remaining: number;
}

export interface QuarantineItem {
  id: number;
  merchantName: string | null;
  documentNumber: string | null;
  processDate: string | null;
  motivo: string;
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  total: number;
}

export interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
  instance?: string;
  traceId?: string;
}
