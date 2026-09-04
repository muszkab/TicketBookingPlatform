import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { HttpErrorResponse } from '@angular/common/http';
import { MatDialog } from '@angular/material/dialog';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { EMPTY, Subject, catchError, filter } from 'rxjs';
import { switchMap } from 'rxjs/operators';

import { OrderStatus, OrderSummaryDto, OrdersService, PagedResultOfOrderSummaryDto } from '../../api';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../core/dialogs/confirm-dialog.component';
import { withActionLock } from '../../core/http/action-lock';
import { mapProblemDetails } from '../../core/http/map-error';
import { NotificationService } from '../../core/notifications/notification.service';

const DEFAULT_PAGE_SIZE = 10;
const PAGE_SIZE_OPTIONS = [5, 10, 25];

interface OrdersQuery {
  readonly page: number;
  readonly pageSize: number;
}

@Component({
  selector: 'app-my-orders',
  imports: [
    DatePipe,
    DecimalPipe,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatTableModule,
    RouterLink
  ],
  templateUrl: './my-orders.component.html',
  styleUrl: './my-orders.component.scss'
})
export class MyOrdersComponent implements OnInit {
  private readonly ordersService = inject(OrdersService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly dialog = inject(MatDialog);
  private readonly notifications = inject(NotificationService);

  protected readonly orders = signal<OrderSummaryDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(DEFAULT_PAGE_SIZE);
  protected readonly cancelling = signal(false);

  protected readonly displayedColumns = ['eventTitle', 'total', 'createdAt', 'status', 'actions'];
  protected readonly pageSizeOptions = PAGE_SIZE_OPTIONS;
  protected readonly statuses = OrderStatus;

  private readonly load$ = new Subject<OrdersQuery>();

  constructor() {
    this.load$
      .pipe(
        switchMap((q) => this.ordersService.getMyOrders(q.page, q.pageSize)),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe({
        next: (result: PagedResultOfOrderSummaryDto) => {
          this.orders.set(result.items ?? []);
          this.totalCount.set(result.totalCount ?? 0);
          this.loading.set(false);
        },
        error: (err: unknown) => {
          console.error('Failed to load my orders', err);
          this.error.set(mapProblemDetails(err, 'Failed to load your orders.'));
          this.loading.set(false);
        }
      });
  }

  ngOnInit(): void {
    this.route.queryParamMap.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((params) => {
      const rawPage = Number(params.get('page'));
      const rawPageSize = Number(params.get('pageSize'));
      this.page.set(Number.isFinite(rawPage) && rawPage > 0 ? rawPage : 1);
      this.pageSize.set(
        PAGE_SIZE_OPTIONS.includes(rawPageSize) ? rawPageSize : DEFAULT_PAGE_SIZE
      );
      this.triggerLoad();
    });
  }

  protected onPageChange(event: PageEvent): void {
    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page: event.pageIndex + 1, pageSize: event.pageSize },
      queryParamsHandling: 'merge'
    });
  }

  protected statusColor(status: OrderStatus): 'primary' | 'accent' | 'warn' | undefined {
    switch (status) {
      case OrderStatus.Paid:
        return 'primary';
      case OrderStatus.Pending:
        return 'accent';
      case OrderStatus.Cancelled:
        return 'warn';
      default:
        return undefined;
    }
  }

  protected continuePaymentLink(order: OrderSummaryDto): (string | number)[] | null {
    return order.status === OrderStatus.Pending ? ['/orders', order.id, 'pay'] : null;
  }

  protected canCancel(order: OrderSummaryDto): boolean {
    return order.status === OrderStatus.Pending;
  }

  protected cancelOrder(order: OrderSummaryDto): void {
    if (!this.canCancel(order) || this.cancelling()) return;

    const data: ConfirmDialogData = {
      title: 'Cancel order?',
      message: 'This will cancel the order and release the reserved tickets. This action cannot be undone.',
      confirmLabel: 'Cancel order',
      cancelLabel: 'Keep order',
      confirmColor: 'warn'
    };

    this.dialog
      .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, { data })
      .afterClosed()
      .pipe(
        filter((confirmed): confirmed is true => confirmed === true),
        switchMap(() =>
          this.ordersService.cancelOrder(order.id).pipe(
            withActionLock(this.cancelling),
            catchError((err: unknown) => {
              if (err instanceof HttpErrorResponse && err.status === 409) {
                this.notifications.error('This order can no longer be cancelled.');
                this.triggerLoad();
              } else {
                console.error('Failed to cancel order', err);
                this.notifications.error(mapProblemDetails(err, 'Failed to cancel the order.'));
              }
              return EMPTY;
            })
          )
        ),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(() => {
        this.notifications.success('Order cancelled.');
        this.triggerLoad();
      });
  }

  protected trackOrder = (_: number, order: OrderSummaryDto): string => order.id;

  private triggerLoad(): void {
    this.loading.set(true);
    this.error.set(null);
    this.load$.next({ page: this.page(), pageSize: this.pageSize() });
  }
}
