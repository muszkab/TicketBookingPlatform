import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import {
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialog, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTabsModule } from '@angular/material/tabs';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { EMPTY, catchError, filter, forkJoin, of, switchMap } from 'rxjs';

import {
  EventDto,
  EventStatus,
  EventsService,
  LocationDto,
  LocationsService,
  ProblemDetails,
  UpdateEventRequest
} from '../../../api';
import {
  ConfirmDialogComponent,
  ConfirmDialogData
} from '../../../core/dialogs/confirm-dialog.component';
import { NotificationService } from '../../../core/notifications/notification.service';
import {
  eventCategoryLabel,
  eventStatusLabel
} from '../../events/shared/event-labels';
import {
  AdminEventFormComponent,
  AdminEventFormValue
} from './admin-event-form.component';

const LOCATION_PAGE_SIZE = 100;

@Component({
  selector: 'app-admin-event-edit',
  imports: [
    DatePipe,
    RouterLink,
    AdminEventFormComponent,
    MatButtonModule,
    MatCardModule,
    MatDialogModule,
    MatIconModule,
    MatProgressBarModule,
    MatTabsModule
  ],
  templateUrl: './admin-event-edit.component.html',
  styleUrl: './admin-event-edit.component.scss'
})
export class AdminEventEditComponent implements OnInit {
  private readonly eventsService = inject(EventsService);
  private readonly locationsService = inject(LocationsService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly dialog = inject(MatDialog);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly event = signal<EventDto | null>(null);
  protected readonly locations = signal<LocationDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);
  protected readonly loadError = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly formError = signal<string | null>(null);
  protected readonly acting = signal(false);

  protected readonly isDraft = computed(() => this.event()?.status === EventStatus.Draft);
  protected readonly canPublish = computed(() => this.event()?.status === EventStatus.Draft);
  protected readonly canCancel = computed(() => {
    const s = this.event()?.status;
    return s !== undefined && s !== EventStatus.Cancelled && s !== EventStatus.Completed;
  });

  protected readonly categoryLabel = eventCategoryLabel;
  protected readonly statusLabel = eventStatusLabel;

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        switchMap((params) => {
          const id = params.get('id');
          this.loading.set(true);
          this.notFound.set(false);
          this.loadError.set(null);
          if (!id) {
            this.notFound.set(true);
            this.loading.set(false);
            return EMPTY;
          }
          return forkJoin({
            event: this.eventsService.getEventById(id).pipe(
              catchError((err: unknown) => {
                if (err instanceof HttpErrorResponse && err.status === 404) {
                  this.notFound.set(true);
                } else {
                  console.error('Failed to load event', err);
                  this.loadError.set('Failed to load event.');
                }
                this.loading.set(false);
                return of<EventDto | null>(null);
              })
            ),
            locations: this.locationsService
              .getLocations(undefined, undefined, 1, LOCATION_PAGE_SIZE)
              .pipe(catchError(() => of({ items: [] as LocationDto[] } as any)))
          });
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(({ event, locations }) => {
        if (event) {
          this.event.set(event);
          this.locations.set(locations.items ?? []);
          this.loading.set(false);
        }
      });
  }

  protected onDetailsSubmit(value: AdminEventFormValue): void {
    const current = this.event();
    if (!current) {
      return;
    }
    const request: UpdateEventRequest = {
      title: value.title,
      description: value.description,
      category: value.category,
      startsAt: value.startsAt,
      endsAt: value.endsAt
    };

    this.submitting.set(true);
    this.formError.set(null);
    this.eventsService
      .updateEvent(current.id, request)
      .pipe(
        switchMap(() => this.eventsService.getEventById(current.id)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (updated) => {
          this.event.set(updated);
          this.submitting.set(false);
          this.notifications.success('Event updated.');
        },
        error: (err: unknown) => {
          this.submitting.set(false);
          console.error('Update failed', err);
          this.formError.set(this.mapError(err, 'Failed to update event.'));
        }
      });
  }

  protected cancelEditing(): void {
    this.router.navigate(['/admin/events']);
  }

  protected publish(): void {
    const current = this.event();
    if (!current) {
      return;
    }
    this.confirm({
      title: 'Publish event',
      message: `Put "${current.title}" on sale? Details and ticket categories will no longer be editable.`,
      confirmLabel: 'Publish'
    })
      .pipe(
        filter((ok) => ok === true),
        switchMap(() => {
          this.acting.set(true);
          return this.eventsService.publishEvent(current.id);
        }),
        switchMap(() => this.eventsService.getEventById(current.id)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (updated) => {
          this.event.set(updated);
          this.acting.set(false);
          this.notifications.success(`"${updated.title}" is now on sale.`);
        },
        error: (err) => {
          this.acting.set(false);
          console.error('Publish failed', err);
          this.notifications.error(this.mapError(err, 'Failed to publish event.'));
        }
      });
  }

  protected cancelEvent(): void {
    const current = this.event();
    if (!current) {
      return;
    }
    this.confirm({
      title: 'Cancel event',
      message: `Cancel "${current.title}"? This cannot be undone.`,
      confirmLabel: 'Cancel event',
      cancelLabel: 'Keep',
      confirmColor: 'warn'
    })
      .pipe(
        filter((ok) => ok === true),
        switchMap(() => {
          this.acting.set(true);
          return this.eventsService.cancelEvent(current.id);
        }),
        switchMap(() => this.eventsService.getEventById(current.id)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (updated) => {
          this.event.set(updated);
          this.acting.set(false);
          this.notifications.success(`"${updated.title}" has been cancelled.`);
        },
        error: (err) => {
          this.acting.set(false);
          console.error('Cancel failed', err);
          this.notifications.error(this.mapError(err, 'Failed to cancel event.'));
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

  private mapError(err: unknown, fallback: string): string {
    if (err instanceof HttpErrorResponse) {
      const problem = err.error as ProblemDetails | undefined;
      if (problem?.detail) {
        return problem.detail;
      }
      if (problem?.title) {
        return problem.title;
      }
      if (err.status === 0) {
        return 'Cannot reach the server.';
      }
    }
    return fallback;
  }
}
