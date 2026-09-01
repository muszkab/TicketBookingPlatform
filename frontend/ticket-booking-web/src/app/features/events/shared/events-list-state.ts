import { DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { PageEvent } from '@angular/material/paginator';
import { ActivatedRoute, Router } from '@angular/router';
import { Subject, switchMap } from 'rxjs';

import {
  EventCategory,
  EventDto,
  EventStatus,
  EventsService,
  PagedResultOfEventDto
} from '../../../api';
import {
  EVENT_CATEGORY_OPTIONS,
  EVENT_STATUS_OPTIONS,
  eventCategoryLabel,
  eventStatusLabel,
  isEventCategory,
  isEventStatus
} from './event-labels';
import { mapProblemDetails } from '../../../core/http/map-error';

export const DEFAULT_PAGE_SIZE = 20;
export const PAGE_SIZE_OPTIONS: readonly number[] = [10, 20, 50];

interface EventsQuery {
  category: EventCategory | null;
  status: EventStatus | null;
  locationId: string | null;
  page: number;
  pageSize: number;
}

export interface EventsListStateOptions {
  /** When true, the locationId filter is parsed from route params and sent to the API. */
  includeLocation?: boolean;
  /** Prefix used when logging load errors to the console. */
  errorLogPrefix?: string;
  /** User-facing message shown when loading fails. */
  errorMessage?: string;
}

/**
 * Creates the shared state and behaviour for an events list (public or admin).
 * Must be called from an Angular injection context (field initializer / constructor).
 */
export function createEventsListState(options: EventsListStateOptions = {}) {
  const {
    includeLocation = false,
    errorLogPrefix = 'Failed to load events',
    errorMessage = 'Failed to load events.'
  } = options;

  const eventsService = inject(EventsService);
  const route = inject(ActivatedRoute);
  const router = inject(Router);
  const destroyRef = inject(DestroyRef);

  const events = signal<EventDto[]>([]);
  const totalCount = signal(0);
  const loading = signal(true);
  const error = signal<string | null>(null);

  const category = signal<EventCategory | null>(null);
  const status = signal<EventStatus | null>(null);
  const locationId = signal<string | null>(null);
  const page = signal(1);
  const pageSize = signal(DEFAULT_PAGE_SIZE);

  const load$ = new Subject<EventsQuery>();

  load$
    .pipe(
      switchMap((q) =>
        eventsService.getEvents(
          q.category ?? undefined,
          q.status ?? undefined,
          q.locationId ?? undefined,
          q.page,
          q.pageSize
        )
      ),
      takeUntilDestroyed(destroyRef)
    )
    .subscribe({
      next: (result: PagedResultOfEventDto) => {
        events.set(result.items ?? []);
        totalCount.set(result.totalCount ?? 0);
        loading.set(false);
      },
      error: (err: unknown) => {
        console.error(errorLogPrefix, err);
        error.set(mapProblemDetails(err, errorMessage));
        loading.set(false);
      }
    });

  const triggerLoad = (): void => {
    loading.set(true);
    error.set(null);
    load$.next({
      category: category(),
      status: status(),
      locationId: includeLocation ? locationId() : null,
      page: page(),
      pageSize: pageSize()
    });
  };

  const updateQueryParams = (
    patch: Partial<{
      category: EventCategory | null;
      status: EventStatus | null;
      locationId: string | null;
      page: number;
      pageSize: number;
    }>
  ): void => {
    const current: Record<string, string | number | null> = {
      category: category(),
      status: status(),
      page: page(),
      pageSize: pageSize()
    };
    if (includeLocation) {
      current['locationId'] = locationId();
    }

    router.navigate([], {
      relativeTo: route,
      queryParams: { ...current, ...patch },
      queryParamsHandling: 'merge'
    });
  };

  const initFromRoute = (): void => {
    route.queryParamMap.pipe(takeUntilDestroyed(destroyRef)).subscribe((params) => {
      const rawCategory = params.get('category');
      const rawStatus = params.get('status');
      const rawPage = Number(params.get('page'));
      const rawPageSize = Number(params.get('pageSize'));

      category.set(isEventCategory(rawCategory) ? rawCategory : null);
      status.set(isEventStatus(rawStatus) ? rawStatus : null);

      if (includeLocation) {
        const rawLocation = params.get('locationId');
        locationId.set(rawLocation && rawLocation.length > 0 ? rawLocation : null);
      }

      page.set(Number.isFinite(rawPage) && rawPage > 0 ? rawPage : 1);
      pageSize.set(
        PAGE_SIZE_OPTIONS.includes(rawPageSize) ? rawPageSize : DEFAULT_PAGE_SIZE
      );

      triggerLoad();
    });
  };

  return {
    // state
    events,
    totalCount,
    loading,
    error,
    category,
    status,
    locationId,
    page,
    pageSize,

    // constants / helpers exposed to templates
    categoryOptions: EVENT_CATEGORY_OPTIONS,
    statusOptions: EVENT_STATUS_OPTIONS,
    pageSizeOptions: PAGE_SIZE_OPTIONS,
    categoryLabel: eventCategoryLabel,
    statusLabel: eventStatusLabel,

    // actions
    initFromRoute,
    reload: triggerLoad,
    triggerLoad,
    onCategoryChange: (value: EventCategory | null) =>
      updateQueryParams({ category: value, page: 1 }),
    onStatusChange: (value: EventStatus | null) =>
      updateQueryParams({ status: value, page: 1 }),
    onLocationChange: (value: string | null) =>
      updateQueryParams({ locationId: value, page: 1 }),
    onPageChange: (event: PageEvent) =>
      updateQueryParams({ page: event.pageIndex + 1, pageSize: event.pageSize })
  };
}
