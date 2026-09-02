import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { EMPTY, catchError, map, of, switchMap } from 'rxjs';

import {
  EventDto,
  EventsService,
  OrderDto,
  OrderItemDto,
  OrderStatus,
  OrdersService
} from '../../api';
import { withActionLock } from '../../core/http/action-lock';
import { mapProblemDetails } from '../../core/http/map-error';
import { NotificationService } from '../../core/notifications/notification.service';

@Component({
  selector: 'app-order-payment',
  imports: [
    DatePipe,
    DecimalPipe,
    MatButtonModule,
    MatCardModule,
    MatIconModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatTableModule,
    RouterLink
  ],
  templateUrl: './order-payment.component.html',
  styleUrl: './order-payment.component.scss'
})
export class OrderPaymentComponent implements OnInit {
  private readonly ordersService = inject(OrdersService);
  private readonly eventsService = inject(EventsService);
  private readonly notifications = inject(NotificationService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly order = signal<OrderDto | null>(null);
  protected readonly event = signal<EventDto | null>(null);
  protected readonly loading = signal(true);
  protected readonly submitting = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly notFound = signal(false);

  protected readonly itemColumns = ['name', 'quantity', 'unitPrice', 'lineTotal'];

  protected readonly canPay = computed(
    () => this.order()?.status === OrderStatus.Pending && !this.submitting()
  );

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        switchMap((params) => {
          const id = params.get('id');
          this.loading.set(true);
          this.error.set(null);
          this.notFound.set(false);
          this.order.set(null);
          this.event.set(null);

          if (!id) {
            this.notFound.set(true);
            this.loading.set(false);
            return EMPTY;
          }

          return this.ordersService.getOrderById(id).pipe(
            switchMap((order) =>
              this.eventsService.getEventById(order.eventId).pipe(
                catchError(() => of<EventDto | null>(null)),
                map((event) => ({ order, event }))
              )
            ),
            catchError((err: unknown) => {
              if (err instanceof HttpErrorResponse && err.status === 404) {
                this.notFound.set(true);
              } else {
                console.error('Failed to load order', err);
                this.error.set('Failed to load the order.');
              }
              this.loading.set(false);
              return EMPTY;
            })
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(({ order, event }) => {
        this.order.set(order);
        this.event.set(event);
        this.loading.set(false);

        if (order.status !== OrderStatus.Pending) {
          this.router.navigate(['/orders', order.id, 'confirmation'], { replaceUrl: true });
        }
      });
  }

  protected lineTotal(item: OrderItemDto): number {
    return item.lineTotalAmount;
  }

  protected trackItem = (_: number, item: OrderItemDto): string => item.id;

  protected simulatePayment(): void {
    const current = this.order();
    if (!current || !this.canPay()) {
      return;
    }

    this.ordersService
      .payOrder(current.id)
      .pipe(withActionLock(this.submitting), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (paid) => {
          this.notifications.success('Payment successful.');
          this.router.navigate(['/orders', paid.id, 'confirmation'], { replaceUrl: true });
        },
        error: (err: unknown) => {
          this.notifications.error(mapProblemDetails(err, 'Payment failed. Please try again.'));
        }
      });
  }
}
