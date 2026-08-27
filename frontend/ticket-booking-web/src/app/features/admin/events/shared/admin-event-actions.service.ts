import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, WritableSignal, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { EMPTY, Observable, catchError, filter, switchMap, tap } from 'rxjs';

import { EventDto, EventsService, ProblemDetails } from '../../../../api';
import {
  ConfirmDialogComponent,
  ConfirmDialogData
} from '../../../../core/dialogs/confirm-dialog.component';
import { NotificationService } from '../../../../core/notifications/notification.service';

@Injectable({ providedIn: 'root' })
export class AdminEventActionsService {
  private readonly eventsService = inject(EventsService);
  private readonly dialog = inject(MatDialog);
  private readonly notifications = inject(NotificationService);

  publish(evt: EventDto, acting?: WritableSignal<boolean>): Observable<EventDto> {
    return this.run(
      {
        title: 'Publish event',
        message: `Put "${evt.title}" on sale? Details and ticket categories will no longer be editable.`,
        confirmLabel: 'Publish'
      },
      () => this.eventsService.publishEvent(evt.id),
      evt,
      (updated) => `"${updated.title}" is now on sale.`,
      'Failed to publish event.',
      'Publish failed',
      acting
    );
  }

  cancel(evt: EventDto, acting?: WritableSignal<boolean>): Observable<EventDto> {
    return this.run(
      {
        title: 'Cancel event',
        message: `Cancel "${evt.title}"? This cannot be undone.`,
        confirmLabel: 'Cancel event',
        cancelLabel: 'Keep',
        confirmColor: 'warn'
      },
      () => this.eventsService.cancelEvent(evt.id),
      evt,
      (updated) => `"${updated.title}" has been cancelled.`,
      'Failed to cancel event.',
      'Cancel failed',
      acting
    );
  }

  mapError(err: unknown, fallback: string): string {
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

  private confirm(data: ConfirmDialogData): Observable<boolean | undefined> {
    return this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, {
        data,
        width: '420px'
      })
      .afterClosed();
  }

  private run(
    confirmData: ConfirmDialogData,
    action: () => Observable<unknown>,
    evt: EventDto,
    successMessage: (updated: EventDto) => string,
    errorFallback: string,
    logPrefix: string,
    acting?: WritableSignal<boolean>
  ): Observable<EventDto> {
    return this.confirm(confirmData).pipe(
      filter((ok): ok is true => ok === true),
      switchMap(() => {
        acting?.set(true);
        return action().pipe(
          switchMap(() => this.eventsService.getEventById(evt.id)),
          catchError((err: unknown) => {
            acting?.set(false);
            console.error(logPrefix, err);
            this.notifications.error(this.mapError(err, errorFallback));
            return EMPTY;
          })
        );
      }),
      // success side effects (notify + reset acting) are attached here so they run
      // only on the successful branch.
      tap((updated) => {
        acting?.set(false);
        this.notifications.success(successMessage(updated));
      })
    );
  }
}
