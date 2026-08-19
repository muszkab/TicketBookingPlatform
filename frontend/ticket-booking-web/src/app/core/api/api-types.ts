// Temporary hand-written types — will be replaced by openapi-generator-cli output.

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
}

export interface EventDto {
  id: string;
  title: string;
  description: string;
  category: string;
  status: string;
  startsAt: string;
  endsAt: string;
}
