export interface PaginationParams {
  page: number;
  per_page: number;
}

export interface PaginationMeta {
  count: number;
  page: number;
  per_page: number;
  total_count: number;
}

export interface PaginatedResponse<T> {
  data: T[];
  meta: PaginationMeta;
}