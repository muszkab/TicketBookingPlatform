import { Injectable, computed, inject, signal } from '@angular/core';
import { DestroyRef } from '@angular/core';

const STORAGE_KEY = 'ticket-booking.cart';
export const MAX_TICKETS_PER_ORDER = 10;

export interface CartItem {
  readonly ticketCategoryId: string;
  readonly name: string;
  readonly unitPrice: number;
  readonly quantity: number;
}

export interface CartState {
  readonly eventId: string;
  readonly eventTitle: string;
  readonly currency: string;
  readonly items: readonly CartItem[];
  readonly updatedAt: string;
}

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly destroyRef = inject(DestroyRef);
  private readonly state = signal<CartState | null>(this.readStorage());

  readonly cart = this.state.asReadonly();

  readonly hasItems = computed(() => (this.state()?.items.length ?? 0) > 0);

  readonly itemCount = computed(() =>
    this.state()?.items.reduce((sum, item) => sum + item.quantity, 0) ?? 0
  );

  readonly totalAmount = computed(() =>
    this.state()?.items.reduce((sum, item) => sum + item.quantity * item.unitPrice, 0) ?? 0
  );

  constructor() {
    if (typeof window !== 'undefined') {
      const listener = (event: StorageEvent) => {
        if (event.key !== STORAGE_KEY) {
          return;
        }
        this.state.set(this.readStorage());
      };
      window.addEventListener('storage', listener);
      this.destroyRef.onDestroy(() => window.removeEventListener('storage', listener));
    }
  }

  isSameEvent(eventId: string): boolean {
    const current = this.state();
    return current !== null && current.eventId === eventId;
  }

  hasDifferentEvent(eventId: string): boolean {
    const current = this.state();
    return current !== null && current.items.length > 0 && current.eventId !== eventId;
  }

  setCart(next: Omit<CartState, 'updatedAt'>): void {
    const clamped = this.clampItems(next.items);
    const value: CartState = {
      eventId: next.eventId,
      eventTitle: next.eventTitle,
      currency: next.currency,
      items: clamped,
      updatedAt: new Date().toISOString()
    };
    this.state.set(value);
    this.writeStorage(value);
  }

  clear(): void {
    this.state.set(null);
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      // ignore storage errors
    }
  }

  private clampItems(items: readonly CartItem[]): readonly CartItem[] {
    const filtered = items.filter((i) => i.quantity > 0);
    let running = 0;
    const capped: CartItem[] = [];
    for (const item of filtered) {
      const remaining = Math.max(0, MAX_TICKETS_PER_ORDER - running);
      const qty = Math.min(item.quantity, remaining);
      if (qty <= 0) {
        break;
      }
      capped.push({ ...item, quantity: qty });
      running += qty;
    }
    return capped;
  }

  private writeStorage(value: CartState): void {
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(value));
    } catch {
      // ignore storage errors
    }
  }

  private readStorage(): CartState | null {
    if (typeof localStorage === 'undefined') {
      return null;
    }
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) {
        return null;
      }
      const parsed = JSON.parse(raw) as CartState;
      if (!parsed?.eventId || !Array.isArray(parsed.items)) {
        return null;
      }
      return parsed;
    } catch {
      return null;
    }
  }
}
