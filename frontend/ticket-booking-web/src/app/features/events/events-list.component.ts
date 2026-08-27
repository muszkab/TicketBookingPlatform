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

import { ErrorCardComponent } from '../../core/error-card/error-card.component';

import {
  EventCategory,
  EventDto,
  EventStatus,
  EventsService,
  LocationDto,
  LocationsService,
  PagedResultOfEventDto
} from '../../api';
import {
  EVENT_CATEGORY_OPTIONS,
  EVENT_STATUS_OPTIONS,
  eventCategoryLabel,
  eventStatusLabel
} from './shared/event-labels';

interface EventsQuery {
  category: EventCategory | null;
  status: EventStatus | null;
  locationId: string | null;
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
    RouterLink,
    ErrorCardComponent
  ],
  templateUrl: './events-list.component.html',
  styleUrl: './events-list.component.scss'
})
export class EventsListComponent implements OnInit {
  private readonly eventsService = inject(EventsService);
  private readonly locationsService = inject(LocationsService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly events = signal<EventDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);

  protected readonly category = signal<EventCategory | null>(null);
  protected readonly status = signal<EventStatus | null>(null);
  protected readonly locationId = signal<string | null>(null);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(DEFAULT_PAGE_SIZE);

  protected readonly locations = signal<LocationDto[]>([]);

  protected readonly categoryOptions = EVENT_CATEGORY_OPTIONS;
  protected readonly statusOptions = EVENT_STATUS_OPTIONS;
  protected readonly pageSizeOptions = [10, 20, 50];

  protected readonly categoryLabel = eventCategoryLabel;
  protected readonly statusLabel = eventStatusLabel;

  private readonly load$ = new Subject<EventsQuery>();

  constructor() {
    this.load$
      .pipe(
        switchMap((q) =>
          this.eventsService.getEvents(
            q.category ?? undefined,
            q.status ?? undefined,
            q.locationId ?? undefined,
            q.page,
            q.pageSize
          )
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
          this.error.set('Failed to load events.');
          this.loading.set(false);
        }
      });
  }

  ngOnInit(): void {
    this.locationsService
      .getLocations(undefined, undefined, 1, 100)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (result) => this.locations.set(result.items ?? []),
        error: (err) => console.error('Failed to load locations', err)
      });

    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const rawCategory = params.get('category');
      const rawStatus = params.get('status');
      const rawLocation = params.get('locationId');
      const rawPage = Number(params.get('page'));
      const rawPageSize = Number(params.get('pageSize'));

      this.category.set(this.isCategory(rawCategory) ? rawCategory : null);
      this.status.set(this.isStatus(rawStatus) ? rawStatus : null);
      this.locationId.set(rawLocation && rawLocation.length > 0 ? rawLocation : null);
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

  protected onLocationChange(value: string | null): void {
    this.updateQueryParams({ locationId: value, page: 1 });
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
      locationId: this.locationId(),
      page: this.page(),
      pageSize: this.pageSize()
    });
  }

  private updateQueryParams(patch: Partial<{
    category: EventCategory | null;
    status: EventStatus | null;
    locationId: string | null;
    page: number;
    pageSize: number;
  }>): void {
    const queryParams: Record<string, string | number | null> = {
      category: patch.category !== undefined ? patch.category : this.category(),
      status: patch.status !== undefined ? patch.status : this.status(),
      locationId: patch.locationId !== undefined ? patch.locationId : this.locationId(),
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
    return (
      value !== null &&
      EVENT_CATEGORY_OPTIONS.some((o) => o.value === (value as EventCategory))
    );
  }

  private isStatus(value: string | null): value is EventStatus {
    return (
      value !== null &&
      EVENT_STATUS_OPTIONS.some((o) => o.value === (value as EventStatus))
    );
  }
}
