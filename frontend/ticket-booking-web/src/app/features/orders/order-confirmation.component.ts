import { DatePipe, DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { EMPTY, catchError, map, of, switchMap } from 'rxjs';

import { EventDto, EventsService, OrderDto, OrderItemDto, OrdersService } from '../../api';

@Component({
  selector: 'app-order-confirmation',
  imports: [
    DatePipe,
    DecimalPipe,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatIconModule,
    MatProgressSpinnerModule,
    MatTableModule,
    RouterLink
  ],
  templateUrl: './order-confirmation.component.html',
  styleUrl: './order-confirmation.component.scss'
})
export class OrderConfirmationComponent implements OnInit {
  private readonly ordersService = inject(OrdersService);
  private readonly eventsService = inject(EventsService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly order = signal<OrderDto | null>(null);
  protected readonly event = signal<EventDto | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly notFound = signal(false);

  protected readonly itemColumns = ['name', 'quantity', 'unitPrice', 'lineTotal'];
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
      });
  }

  protected lineTotal(item: OrderItemDto): number {
    return item.lineTotalAmount;
  }

  protected trackItem = (_: number, item: OrderItemDto): string => item.id;
}
