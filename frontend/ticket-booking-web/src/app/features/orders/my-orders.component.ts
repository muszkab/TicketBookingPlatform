import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, OnInit, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatSortModule, Sort, SortDirection as MatSortDirection } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { Subject } from 'rxjs';
import { switchMap } from 'rxjs/operators';

import { OrderSortField, OrderStatus, OrderSummaryDto, OrdersService, PagedResultOfOrderSummaryDto, SortDirection } from '../../api';
import { OrderCancelService } from '../../core/orders/order-cancel.service';
import { mapProblemDetails } from '../../core/http/map-error';

const DEFAULT_PAGE_SIZE = 10;
const PAGE_SIZE_OPTIONS = [5, 10, 25];
const DEFAULT_SORT_BY: OrderSortField = OrderSortField.CreatedAt;
const DEFAULT_SORT_DIR: SortDirection = SortDirection.Desc;

const COLUMN_TO_SORT_FIELD: Readonly<Record<string, OrderSortField>> = {
  eventTitle: OrderSortField.EventTitle,
  total: OrderSortField.TotalAmount,
  createdAt: OrderSortField.CreatedAt,
  status: OrderSortField.Status
};

const SORT_FIELD_TO_COLUMN: Readonly<Record<OrderSortField, string>> = {
  [OrderSortField.EventTitle]: 'eventTitle',
  [OrderSortField.TotalAmount]: 'total',
  [OrderSortField.CreatedAt]: 'createdAt',
  [OrderSortField.Status]: 'status'
};

interface OrdersQuery {
  readonly page: number;
  readonly pageSize: number;
  readonly sortBy: OrderSortField;
  readonly sortDir: SortDirection;
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
    MatSortModule,
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
  private readonly cancelService = inject(OrderCancelService);

  protected readonly orders = signal<OrderSummaryDto[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly page = signal(1);
  protected readonly pageSize = signal(DEFAULT_PAGE_SIZE);
  protected readonly sortBy = signal<OrderSortField>(DEFAULT_SORT_BY);
  protected readonly sortDir = signal<SortDirection>(DEFAULT_SORT_DIR);
  protected readonly cancelling = signal(false);

  protected readonly displayedColumns = ['eventTitle', 'ticketQuantity', 'total', 'createdAt', 'status', 'actions'];
  protected readonly pageSizeOptions = PAGE_SIZE_OPTIONS;
  protected readonly statuses = OrderStatus;

  private readonly load$ = new Subject<OrdersQuery>();

  constructor() {
    this.load$
      .pipe(
        switchMap((q) => this.ordersService.getMyOrders(q.page, q.pageSize, q.sortBy, q.sortDir)),
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
      this.sortBy.set(this.parseSortBy(params.get('sortBy')));
      this.sortDir.set(this.parseSortDir(params.get('sortDir')));
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

  protected onSortChange(sort: Sort): void {
    const sortBy = sort.direction ? COLUMN_TO_SORT_FIELD[sort.active] ?? DEFAULT_SORT_BY : DEFAULT_SORT_BY;
    const sortDir: SortDirection = sort.direction === 'asc' ? SortDirection.Asc : SortDirection.Desc;

    this.router.navigate([], {
      relativeTo: this.route,
      queryParams: { page: 1, sortBy, sortDir },
      queryParamsHandling: 'merge'
    });
  }

  protected get activeSortColumn(): string {
    return SORT_FIELD_TO_COLUMN[this.sortBy()];
  }

  protected get activeSortDirection(): MatSortDirection {
    return this.sortDir() === SortDirection.Asc ? 'asc' : 'desc';
  }

  private parseSortBy(value: string | null): OrderSortField {
    return value && (Object.values(OrderSortField) as string[]).includes(value)
      ? (value as OrderSortField)
      : DEFAULT_SORT_BY;
  }

  private parseSortDir(value: string | null): SortDirection {
    return value === SortDirection.Asc ? SortDirection.Asc : DEFAULT_SORT_DIR;
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

  protected canViewTickets(order: OrderSummaryDto): boolean {
    return order.status === OrderStatus.Paid;
  }

  protected cancelOrder(order: OrderSummaryDto): void {
    if (!this.canCancel(order) || this.cancelling()) return;

    this.cancelService
      .confirmAndCancel(order.id, this.cancelling, { onConflict: () => this.triggerLoad() })
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.triggerLoad());
  }

  protected trackOrder = (_: number, order: OrderSummaryDto): string => order.id;

  private triggerLoad(): void {
    this.loading.set(true);
    this.error.set(null);
    this.load$.next({ page: this.page(), pageSize: this.pageSize(), sortBy: this.sortBy(), sortDir: this.sortDir() });
  }
}
