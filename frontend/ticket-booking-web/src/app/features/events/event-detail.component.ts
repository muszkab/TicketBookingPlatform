import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatChipsModule } from '@angular/material/chips';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatTableModule } from '@angular/material/table';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { MatDialog } from '@angular/material/dialog';
import { EMPTY, catchError, from, map, of, switchMap } from 'rxjs';

import {
  EventDto,
  EventStatus,
  EventsService,
  LocationDto,
  LocationsService,
  TicketCategoryDto
} from '../../api';
import { AuthService } from '../../core/auth/auth.service';
import { CartItem, CartService, MAX_TICKETS_PER_ORDER } from '../../core/cart/cart.service';
import { ConfirmDialogComponent, ConfirmDialogData } from '../../core/dialogs/confirm-dialog.component';
import { eventCategoryLabel, eventStatusLabel } from './shared/event-labels';

interface SelectedTicket {
  readonly category: TicketCategoryDto;
  readonly quantity: number;
  readonly subtotal: number;
}

@Component({
  selector: 'app-event-detail',
  imports: [
    DatePipe,
    DecimalPipe,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatChipsModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressSpinnerModule,
    MatTableModule,
    RouterLink
  ],
  templateUrl: './event-detail.component.html',
  styleUrl: './event-detail.component.scss'
})
export class EventDetailComponent implements OnInit {
  private readonly eventsService = inject(EventsService);
  private readonly locationsService = inject(LocationsService);
  private readonly route = inject(ActivatedRoute);
  private readonly destroyRef = inject(DestroyRef);
  private readonly auth = inject(AuthService);
  private readonly cart = inject(CartService);
  private readonly dialog = inject(MatDialog);
  private readonly router = inject(Router);

  protected readonly event = signal<EventDto | null>(null);
  protected readonly location = signal<LocationDto | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly notFound = signal(false);
  protected readonly quantities = signal<Record<string, number>>({});

  protected readonly isAuthenticated = this.auth.isAuthenticated;
  protected readonly maxPerOrder = MAX_TICKETS_PER_ORDER;

  protected readonly bookingOpen = computed(() => this.event()?.status === EventStatus.OnSale);

  protected readonly selectedItems = computed<SelectedTicket[]>(() => {
    const evt = this.event();
    if (!evt) {
      return [];
    }
    const map = this.quantities();
    const items: SelectedTicket[] = [];
    for (const category of evt.ticketCategories) {
      const qty = map[category.id] ?? 0;
      if (qty > 0) {
        items.push({ category, quantity: qty, subtotal: qty * category.price });
      }
    }
    return items;
  });

  protected readonly totalQuantity = computed(() =>
    this.selectedItems().reduce((sum, item) => sum + item.quantity, 0)
  );

  protected readonly totalAmount = computed(() =>
    this.selectedItems().reduce((sum, item) => sum + item.subtotal, 0)
  );

  protected readonly remainingCapacity = computed(() =>
    Math.max(0, MAX_TICKETS_PER_ORDER - this.totalQuantity())
  );

  protected readonly currency = computed(
    () => this.event()?.ticketCategories[0]?.currency ?? ''
  );

  protected readonly overLimit = computed(() => this.totalQuantity() > MAX_TICKETS_PER_ORDER);

  protected readonly canBook = computed(
    () =>
      this.isAuthenticated() &&
      this.bookingOpen() &&
      this.totalQuantity() > 0 &&
      !this.overLimit()
  );

  protected readonly ticketColumns = ['name', 'price', 'available', 'quantity'];

  protected readonly categoryLabel = eventCategoryLabel;
  protected readonly statusLabel = eventStatusLabel;

  ngOnInit(): void {
    this.route.paramMap
      .pipe(
        switchMap((params) => {
          const id = params.get('id');
          this.loading.set(true);
          this.error.set(null);
          this.notFound.set(false);
          this.event.set(null);
          this.location.set(null);

          if (!id) {
            this.notFound.set(true);
            this.loading.set(false);
            return EMPTY;
          }

          return this.eventsService.getEventById(id).pipe(
            switchMap((evt) =>
              this.locationsService.getLocationById(evt.locationId).pipe(
                catchError(() => of<LocationDto | null>(null)),
                map((location) => ({ event: evt, location }))
              )
            ),
            catchError((err: unknown) => {
              if (err instanceof HttpErrorResponse && err.status === 404) {
                this.notFound.set(true);
              } else {
                console.error('Failed to load event', err);
                this.error.set('Failed to load event. Please check your connection and try again.');
              }
              this.loading.set(false);
              return EMPTY;
            })
          );
        }),
        takeUntilDestroyed(this.destroyRef)
      )
      .subscribe(({ event, location }) => {
        this.event.set(event);
        this.location.set(location);
        const initial: Record<string, number> = {};
        for (const category of event.ticketCategories) {
          initial[category.id] = 0;
        }
        if (this.cart.isSameEvent(event.id)) {
          const existing = this.cart.cart();
          if (existing) {
            for (const item of existing.items) {
              if (item.ticketCategoryId in initial) {
                const category = event.ticketCategories.find((c) => c.id === item.ticketCategoryId);
                const max = Math.min(category?.availableQuantity ?? 0, MAX_TICKETS_PER_ORDER);
                initial[item.ticketCategoryId] = Math.min(item.quantity, max);
              }
            }
          }
        }
        this.quantities.set(initial);
        this.loading.set(false);
      });
  }

  protected updateQuantity(category: TicketCategoryDto, raw: number | string | null): void {
    const parsed = typeof raw === 'number' ? raw : Number.parseInt(String(raw ?? '0'), 10);
    const numeric = Number.isFinite(parsed) ? Math.floor(parsed) : 0;
    const current = this.quantities();
    const otherTotal = Object.entries(current).reduce(
      (sum, [id, qty]) => (id === category.id ? sum : sum + (qty ?? 0)),
      0
    );
    const remainingBudget = Math.max(0, MAX_TICKETS_PER_ORDER - otherTotal);
    const capped = Math.max(0, Math.min(numeric, category.availableQuantity, remainingBudget));
    this.quantities.set({ ...current, [category.id]: capped });
  }

  protected bookSelected(): void {
    const evt = this.event();
    if (!evt || !this.canBook()) {
      return;
    }

    const proceed$ = this.cart.hasDifferentEvent(evt.id)
      ? from(
          this.dialog
            .open<ConfirmDialogComponent, ConfirmDialogData, boolean>(ConfirmDialogComponent, {
              data: {
                title: 'Replace cart?',
                message:
                  'Your cart already contains tickets for a different event. Continuing will replace them with your new selection.',
                confirmLabel: 'Replace',
                confirmColor: 'warn'
              }
            })
            .afterClosed()
        ).pipe(map((result) => result === true))
      : of(true);

    proceed$.pipe(takeUntilDestroyed(this.destroyRef)).subscribe((ok) => {
      if (!ok) {
        return;
      }
      const items: CartItem[] = this.selectedItems().map((s) => ({
        ticketCategoryId: s.category.id,
        name: s.category.name,
        unitPrice: s.category.price,
        quantity: s.quantity
      }));
      this.cart.setCart({
        eventId: evt.id,
        eventTitle: evt.title,
        currency: this.currency(),
        items
      });
      this.router.navigate(['/checkout']);
    });
  }

  protected trackTicket = (_: number, item: TicketCategoryDto): string => item.id;
}
