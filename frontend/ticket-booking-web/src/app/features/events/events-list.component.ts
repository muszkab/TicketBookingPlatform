import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatOptionModule } from '@angular/material/core';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSelectModule } from '@angular/material/select';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject } from 'rxjs';
import { switchMap } from 'rxjs/operators';

import {
  EventCategory,
  EventDto,
  EventStatus,
  EventsService,
  PagedResultOfEventDto
} from '../../api';

interface EventsQuery {
  category: EventCategory | null;
  status: EventStatus | null;
  page: number;
  pageSize: number;
}

const DEFAULT_PAGE_SIZE = 20;

@Component({
  selector: 'app-events-list',
  imports: [
    DatePipe,
    MatButtonModule,
    MatCardModule,
    MatFormFieldModule,
    MatOptionModule,
    MatPaginatorModule,
    MatProgressSpinnerModule,
    MatSelectModule,
    RouterLink
  ],
  templateUrl: './events-list.component.html',
  styleUrl: './events-list.component.scss'
})
export class EventsListComponent implements OnInit {
  private readonly eventsService = inject(EventsService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly events = signal<EventDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  protected readonly category = signal<EventCategory | null>(null);
  protected readonly status = signal<EventStatus | null>(null);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(DEFAULT_PAGE_SIZE);

  protected readonly categories = Object.values(EventCategory);
  protected readonly statuses = Object.values(EventStatus);
  protected readonly pageSizeOptions = [10, 20, 50];

  private readonly load$ = new Subject<EventsQuery>();

  constructor() {
    this.load$
      .pipe(
        switchMap((q) =>
          this.eventsService.getEvents(q.category ?? undefined, q.status ?? undefined, q.page, q.pageSize)
        ),
        takeUntilDestroyed()
      )
      .subscribe({
        next: (result: PagedResultOfEventDto) => {
          this.events.set(result.items ?? []);
          this.totalCount.set(result.totalCount ?? 0);
          this.loading.set(false);
        },
        error: (err: unknown) => {
          console.error('Failed to load events', err);
          this.error.set('Failed to load events. Is the backend running on https://localhost:5001?');
          this.loading.set(false);
        }
      });
  }

  ngOnInit(): void {
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const rawCategory = params.get('category');
      const rawStatus = params.get('status');
      const rawPage = Number(params.get('page'));
      const rawPageSize = Number(params.get('pageSize'));

      this.category.set(this.isCategory(rawCategory) ? rawCategory : null);
      this.status.set(this.isStatus(rawStatus) ? rawStatus : null);
      this.page.set(Number.isFinite(rawPage) && rawPage > 0 ? rawPage : 1);
      this.pageSize.set(
        this.pageSizeOptions.includes(rawPageSize) ? rawPageSize : DEFAULT_PAGE_SIZE
      );

      this.triggerLoad();
    });
  }

  protected reload(): void {
    this.triggerLoad();
  }

  protected onCategoryChange(value: EventCategory | null): void {
    this.updateQueryParams({ category: value, page: 1 });
  }

  protected onStatusChange(value: EventStatus | null): void {
    this.updateQueryParams({ status: value, page: 1 });
  }

  protected onPageChange(event: PageEvent): void {
    this.updateQueryParams({
      page: event.pageIndex + 1,
      pageSize: event.pageSize
    });
  }

  private triggerLoad(): void {
    this.loading.set(true);
    this.error.set(null);
    this.load$.next({
      category: this.category(),
      status: this.status(),
      page: this.page(),
      pageSize: this.pageSize()
    });
  }

  private updateQueryParams(patch: Partial<{
    category: EventCategory | null;
    status: EventStatus | null;
    page: number;
    pageSize: number;
  }>): void {
    const queryParams: Record<string, string | number | null> = {
      category: patch.category !== undefined ? patch.category : this.category(),
      status: patch.status !== undefined ? patch.status : this.status(),
      page: patch.page ?? this.page(),
      pageSize: patch.pageSize ?? this.pageSize()
    };

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams,
      queryParamsHandling: 'merge'
    });
  }

  private isCategory(value: string | null): value is EventCategory {
    return value !== null && (this.categories as string[]).includes(value);
  }

  private isStatus(value: string | null): value is EventStatus {
    return value !== null && (this.statuses as string[]).includes(value);
  }
}
