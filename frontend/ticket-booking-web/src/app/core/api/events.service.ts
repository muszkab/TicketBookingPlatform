import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { environment } from '../../../environments/environment';
import { EventDto, PagedResult } from './api-types';

@Injectable({ providedIn: 'root' })
export class EventsService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = `${environment.apiBaseUrl}/Events`;

  getAll(page = 1, pageSize = 10): Observable<PagedResult<EventDto>> {
    return this.http.get<PagedResult<EventDto>>(this.baseUrl, {
      params: { page, pageSize }
    });
  }
}
