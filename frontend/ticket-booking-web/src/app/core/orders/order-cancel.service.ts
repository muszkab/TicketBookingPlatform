import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, WritableSignal, inject } from '@angular/core';
import { MatDialog } from '@angular/material/dialog';
import { EMPTY, Observable, catchError, filter, switchMap, tap } from 'rxjs';

import { OrderDto, OrdersService } from '../../api';
import { ConfirmDialogComponent, ConfirmDialogData } from '../dialogs/confirm-dialog.component';
import { withActionLock } from '../http/action-lock';
import { mapProblemDetails } from '../http/map-error';
import { NotificationService } from '../notifications/notification.service';

export interface CancelOrderOptions {
  readonly onConflict?: () => void;
  readonly onNotFound?: () => void;
}

const CONFIRM_DATA: ConfirmDialogData = {
  title: 'Cancel order?',
  message: 'This will cancel the order and release the reserved tickets. This action cannot be undone.',
  confirmLabel: 'Cancel order',
  cancelLabel: 'Keep order',
  confirmColor: 'warn'
};

@Injectable({ providedIn: 'root' })
export class OrderCancelService {
  private readonly dialog = inject(MatDialog);
  private readonly ordersService = inject(OrdersService);
  private readonly notifications = inject(NotificationService);

  confirmAndCancel(
    orderId: string,
    lock: WritableSignal<boolean>,
    options?: CancelOrderOptions
  ): Observable<OrderDto> {
    return this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, {
        data: CONFIRM_DATA
      })
      .afterClosed()
      .pipe(
        filter((confirmed): confirmed is true => confirmed === true),
        switchMap(() =>
          this.ordersService.cancelOrder(orderId).pipe(
            withActionLock(lock),
            tap(() => this.notifications.success('Order cancelled.')),
            catchError((err: unknown) => {
              this.handleError(err, options);
              return EMPTY;
            })
          )
        )
      );
  }

  private handleError(err: unknown, options?: CancelOrderOptions): void {
    if (err instanceof HttpErrorResponse && err.status === 409) {
      this.notifications.error('This order can no longer be cancelled.');
      options?.onConflict?.();
      return;
    }

    if (err instanceof HttpErrorResponse && err.status === 404) {
      this.notifications.error('Order not found.');
      options?.onNotFound?.();
      return;
    }

    console.error('Failed to cancel order', err);
    this.notifications.error(mapProblemDetails(err, 'Failed to cancel the order.'));
  }
}
