import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, DestroyRef, OnInit, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatCheckboxModule } from '@angular/material/checkbox';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatTableModule } from '@angular/material/table';
import { Router, RouterLink } from '@angular/router';

import { EventDto, EventsService } from '../../api';
import { AuthService } from '../../core/auth/auth.service';
import { CartItem, CartService } from '../../core/cart/cart.service';
import { CheckoutError, CheckoutService } from '../../core/cart/checkout.service';
import { withActionLock } from '../../core/http/action-lock';
import { NotificationService } from '../../core/notifications/notification.service';

@Component({
  selector: 'app-checkout',
  imports: [
    DatePipe,
    DecimalPipe,
    FormsModule,
    MatButtonModule,
    MatCardModule,
    MatCheckboxModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatProgressBarModule,
    MatTableModule,
    RouterLink
  ],
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.scss'
})
export class CheckoutComponent implements OnInit {
  private readonly cart = inject(CartService);
  private readonly checkout = inject(CheckoutService);
  private readonly auth = inject(AuthService);
  private readonly eventsService = inject(EventsService);
  private readonly notifications = inject(NotificationService);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly cartState = this.cart.cart;
  protected readonly totalAmount = this.cart.totalAmount;
  protected readonly email = signal<string>(this.auth.userName() ?? '');
  protected readonly termsAccepted = signal(false);
  protected readonly submitting = signal(false);
  protected readonly event = signal<EventDto | null>(null);

  protected readonly itemColumns = ['name', 'quantity', 'unitPrice', 'lineTotal'];

  protected readonly canPlaceOrder = computed(
    () => !!this.cartState() && this.termsAccepted() && !this.submitting()
  );

  ngOnInit(): void {
    const current = this.cartState();
    if (!current || current.items.length === 0) {
      this.router.navigate(['/events']);
      return;
    }

    this.eventsService
      .getEventById(current.eventId)
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (evt) => this.event.set(evt),
        error: () => this.event.set(null)
      });
  }

  protected lineTotal(item: CartItem): number {
    return item.quantity * item.unitPrice;
  }

  protected trackItem = (_: number, item: CartItem): string => item.ticketCategoryId;

  protected placeOrder(): void {
    const current = this.cartState();
    if (!current || !this.canPlaceOrder()) {
      return;
    }

    this.checkout
      .placeOrder(current)
      .pipe(withActionLock(this.submitting), takeUntilDestroyed(this.destroyRef))
      .subscribe({
        next: (order) => {
          this.cart.clear();
          this.notifications.success('Booking confirmed.');
          this.router.navigate(['/orders', order.id, 'confirmation']);
        },
        error: (err: CheckoutError) => {
          if (err.kind === 'unauthorized') {
            this.notifications.error(err.message);
            this.router.navigate(['/login'], { queryParams: { returnUrl: '/checkout' } });
            return;
          }
          this.notifications.error(err.message);
          if (err.kind === 'conflict' || err.kind === 'notFound') {
            const eventId = current.eventId;
            this.cart.clear();
            this.router.navigate(['/events', eventId]);
          }
        }
      });
  }
}
