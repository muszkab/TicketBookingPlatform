import { TestBed } from '@angular/core/testing';

import { CartService, MAX_TICKETS_PER_ORDER } from './cart.service';

describe('CartService', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
  });

  afterEach(() => {
    localStorage.clear();
  });

  it('starts with an empty cart when nothing is stored', () => {
    const service = TestBed.inject(CartService);

    expect(service.cart()).toBeNull();
    expect(service.hasItems()).toBe(false);
    expect(service.itemCount()).toBe(0);
    expect(service.totalAmount()).toBe(0);
  });

  it('setCart stores items and computes totals', () => {
    const service = TestBed.inject(CartService);

    service.setCart({
      eventId: 'event-1',
      eventTitle: 'Concert',
      currency: 'HUF',
      items: [
        { ticketCategoryId: 'vip', name: 'VIP', unitPrice: 100, quantity: 2 },
        { ticketCategoryId: 'std', name: 'Standard', unitPrice: 50, quantity: 3 }
      ]
    });

    expect(service.hasItems()).toBe(true);
    expect(service.itemCount()).toBe(5);
    expect(service.totalAmount()).toBe(350);
    expect(service.cart()?.eventId).toBe('event-1');
  });

  it('persists the cart to localStorage', () => {
    const service = TestBed.inject(CartService);

    service.setCart({
      eventId: 'event-1',
      eventTitle: 'Concert',
      currency: 'HUF',
      items: [{ ticketCategoryId: 'vip', name: 'VIP', unitPrice: 100, quantity: 1 }]
    });

    const stored = JSON.parse(localStorage.getItem('ticket-booking.cart')!);
    expect(stored.eventId).toBe('event-1');
    expect(stored.items).toHaveLength(1);
  });

  it('filters out zero-quantity items', () => {
    const service = TestBed.inject(CartService);

    service.setCart({
      eventId: 'event-1',
      eventTitle: 'Concert',
      currency: 'HUF',
      items: [
        { ticketCategoryId: 'vip', name: 'VIP', unitPrice: 100, quantity: 0 },
        { ticketCategoryId: 'std', name: 'Standard', unitPrice: 50, quantity: 2 }
      ]
    });

    expect(service.cart()?.items).toEqual([
      { ticketCategoryId: 'std', name: 'Standard', unitPrice: 50, quantity: 2 }
    ]);
  });

  it('caps the total quantity at MAX_TICKETS_PER_ORDER', () => {
    const service = TestBed.inject(CartService);

    service.setCart({
      eventId: 'event-1',
      eventTitle: 'Concert',
      currency: 'HUF',
      items: [
        { ticketCategoryId: 'a', name: 'A', unitPrice: 10, quantity: 7 },
        { ticketCategoryId: 'b', name: 'B', unitPrice: 10, quantity: 7 }
      ]
    });

    expect(service.itemCount()).toBe(MAX_TICKETS_PER_ORDER);
    expect(service.cart()?.items).toEqual([
      { ticketCategoryId: 'a', name: 'A', unitPrice: 10, quantity: 7 },
      { ticketCategoryId: 'b', name: 'B', unitPrice: 10, quantity: 3 }
    ]);
  });

  it('isSameEvent and hasDifferentEvent reflect the current cart', () => {
    const service = TestBed.inject(CartService);

    service.setCart({
      eventId: 'event-1',
      eventTitle: 'Concert',
      currency: 'HUF',
      items: [{ ticketCategoryId: 'a', name: 'A', unitPrice: 10, quantity: 1 }]
    });

    expect(service.isSameEvent('event-1')).toBe(true);
    expect(service.isSameEvent('event-2')).toBe(false);
    expect(service.hasDifferentEvent('event-2')).toBe(true);
    expect(service.hasDifferentEvent('event-1')).toBe(false);
  });

  it('clear empties the cart and localStorage', () => {
    const service = TestBed.inject(CartService);

    service.setCart({
      eventId: 'event-1',
      eventTitle: 'Concert',
      currency: 'HUF',
      items: [{ ticketCategoryId: 'a', name: 'A', unitPrice: 10, quantity: 1 }]
    });

    service.clear();

    expect(service.cart()).toBeNull();
    expect(localStorage.getItem('ticket-booking.cart')).toBeNull();
  });

  it('loads a previously stored cart from localStorage on creation', () => {
    localStorage.setItem(
      'ticket-booking.cart',
      JSON.stringify({
        eventId: 'event-9',
        eventTitle: 'Old event',
        currency: 'EUR',
        items: [{ ticketCategoryId: 'x', name: 'X', unitPrice: 5, quantity: 1 }],
        updatedAt: new Date().toISOString()
      })
    );

    const service = TestBed.inject(CartService);

    expect(service.cart()?.eventId).toBe('event-9');
    expect(service.hasItems()).toBe(true);
  });
});
