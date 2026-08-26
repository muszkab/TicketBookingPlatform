import { DatePipe } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatOptionModule } from '@angular/material/core';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatMenuModule } from '@angular/material/menu';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSelectModule } from '@angular/material/select';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject, filter, switchMap } from 'rxjs';

import {
  EventCategory,
  EventDto,
  EventStatus,
  EventsService,
  PagedResultOfEventDto
} from '../../../api';
import {
  ConfirmDialogComponent,
  ConfirmDialogData
} from '../../../core/dialogs/confirm-dialog.component';
import { NotificationService } from '../../../core/notifications/notification.service';
import {
  EVENT_CATEGORY_OPTIONS,
  EVENT_STATUS_OPTIONS,
  eventCategoryLabel,
  eventStatusLabel
} from './event-labels';

interface EventsQuery {
  category: EventCategory | null;
  status: EventStatus | null;
  page: number;
  pageSize: number;
}

const DEFAULT_PAGE_SIZE = 20;

@Component({
  selector: 'app-admin-events-list',
  imports: [
    DatePipe,
    RouterLink,
    MatButtonModule,
    MatDialogModule,
    MatFormFieldModule,
    MatIconModule,
    MatMenuModule,
    MatOptionModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatSelectModule,
    MatTableModule,
    MatTooltipModule
  ],
  templateUrl: './admin-events-list.component.html',
  styleUrl: './admin-events-list.component.scss'
})
export class AdminEventsListComponent implements OnInit {
  private readonly eventsService = inject(EventsService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly events = signal<EventDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly acting = signal(false);

  protected readonly category = signal<EventCategory | null>(null);
  protected readonly status = signal<EventStatus | null>(null);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(DEFAULT_PAGE_SIZE);

  protected readonly categoryOptions = EVENT_CATEGORY_OPTIONS;
  protected readonly statusOptions = EVENT_STATUS_OPTIONS;
  protected readonly pageSizeOptions = [10, 20, 50];
  protected readonly displayedColumns = [
    'title',
    'category',
    'status',
    'startsAt',
    'actions'
  ] as const;

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
          console.error('Failed to load admin events', err);
          this.error.set('Failed to load events.');
          this.loading.set(false);
        }
      });
  }

  ngOnInit(): void {
    this.route.queryParamMap
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((params) => {
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

  protected canPublish(evt: EventDto): boolean {
    return evt.status === EventStatus.Draft;
  }

  protected canCancel(evt: EventDto): boolean {
    return (
      evt.status !== EventStatus.Cancelled && evt.status !== EventStatus.Completed
    );
  }

  protected publish(evt: EventDto): void {
    this.confirm({
      title: 'Publish event',
      message: `Put "${evt.title}" on sale? Details and ticket categories will no longer be editable.`,
      confirmLabel: 'Publish',
      cancelLabel: 'Cancel'
    })
      .pipe(
        filter((ok) => ok === true),
        switchMap(() => {
          this.acting.set(true);
          return this.eventsService.publishEvent(evt.id);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.acting.set(false);
          this.notifications.success(`"${evt.title}" is now on sale.`);
          this.triggerLoad();
        },
        error: (err) => {
          this.acting.set(false);
          console.error('Publish failed', err);
          this.notifications.error('Failed to publish event.');
        }
      });
  }

  protected cancelEvent(evt: EventDto): void {
    this.confirm({
      title: 'Cancel event',
      message: `Cancel "${evt.title}"? This cannot be undone.`,
      confirmLabel: 'Cancel event',
      cancelLabel: 'Keep',
      confirmColor: 'warn'
    })
      .pipe(
        filter((ok) => ok === true),
        switchMap(() => {
          this.acting.set(true);
          return this.eventsService.cancelEvent(evt.id);
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: () => {
          this.acting.set(false);
          this.notifications.success(`"${evt.title}" has been cancelled.`);
          this.triggerLoad();
        },
        error: (err) => {
          this.acting.set(false);
          console.error('Cancel failed', err);
          this.notifications.error('Failed to cancel event.');
        }
      });
  }

  private confirm(data: ConfirmDialogData) {
    return this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, {
        data,
        width: '420px'
      })
      .afterClosed();
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

  private updateQueryParams(
    patch: Partial<{
      category: EventCategory | null;
      status: EventStatus | null;
      page: number;
      pageSize: number;
    }>
  ): void {
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
