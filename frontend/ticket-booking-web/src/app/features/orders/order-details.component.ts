import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { EMPTY, catchError, switchMap } from 'rxjs';

import { OrderDto, OrderItemDto, OrderStatus, OrdersService } from '../../api';
import { OrderCancelService } from '../../core/orders/order-cancel.service';

interface StatusPresentation {
  readonly icon: string;
  readonly color: 'primary' | 'accent' | 'warn' | undefined;
  readonly label: string;
}

@Component({
  selector: 'app-order-details',
  imports: [
    DatePipe,
    DecimalPipe,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatTableModule,
    RouterLink
  ],
  templateUrl: './order-details.component.html',
  styleUrl: './order-details.component.scss'
})
export class OrderDetailsComponent implements OnInit {
  private readonly ordersService = inject(OrdersService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly cancelService = inject(OrderCancelService);

  protected readonly order = signal<OrderDto | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly notFound = signal(false);
  protected readonly cancelling = signal(false);

  protected readonly itemColumns = ['name', 'quantity', 'unitPrice', 'lineTotal'];

  protected readonly statusPresentation = computed<StatusPresentation | null>(() => {
    const ord = this.order();
    if (!ord) return null;
    switch (ord.status) {
      case OrderStatus.Paid:
        return { icon: 'check_circle', color: 'primary', label: ord.status };
      case OrderStatus.Cancelled:
        return { icon: 'cancel', color: 'warn', label: ord.status };
      case OrderStatus.Pending:
      default:
        return { icon: 'hourglass_empty', color: 'accent', label: ord.status };
    }
  });

  protected readonly canPay = computed(() => this.order()?.status === OrderStatus.Pending);
  protected readonly canCancel = computed(() => this.order()?.status === OrderStatus.Pending);

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        switchMap((params) => {
          const id = params.get('id');
          this.loading.set(true);
          this.error.set(null);
          this.notFound.set(false);
          this.order.set(null);

          if (!id) {
            this.notFound.set(true);
            this.loading.set(false);
            return EMPTY;
          }

          return this.ordersService.getOrderById(id).pipe(
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
      .subscribe((order) => {
        this.order.set(order);
        this.loading.set(false);
      });
  }

  protected cancelOrder(): void {
    const ord = this.order();
    if (!ord || !this.canCancel() || this.cancelling()) return;

    this.cancelService
      .confirmAndCancel(ord.id, this.cancelling, {
        onConflict: () => this.reload(ord.id),
        onNotFound: () => {
          this.notFound.set(true);
          this.order.set(null);
        }
      })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((updated) => this.order.set(updated));
  }

  private reload(id: string): void {
    this.ordersService
      .getOrderById(id)
      .pipe(
        catchError(() => EMPTY),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe((order) => this.order.set(order));
  }

  protected lineTotal(item: OrderItemDto): number {
    return item.lineTotalAmount;
  }

  protected trackItem = (_: number, item: OrderItemDto): string => item.id;
}
