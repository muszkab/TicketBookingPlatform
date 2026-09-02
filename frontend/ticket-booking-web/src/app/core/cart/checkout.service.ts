import { HttpErrorResponse } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { CreateOrderRequest, OrderDto, OrdersService } from '../../api';
import { mapProblemDetails } from '../http/map-error';
import { CartState } from './cart.service';

export interface CheckoutError {
  readonly kind: 'validation' | 'notFound' | 'conflict' | 'unauthorized' | 'unknown';
  readonly message: string;
  readonly status: number;
}

@Injectable({ providedIn: 'root' })
export class CheckoutService {
  private readonly orders = inject(OrdersService);

  placeOrder(cart: CartState): Observable<OrderDto> {
    const request: CreateOrderRequest = {
      eventId: cart.eventId,
      currency: cart.currency,
      items: cart.items.map((item) => ({
        ticketCategoryId: item.ticketCategoryId,
        quantity: item.quantity
      }))
    };

    return this.orders.createOrder(request).pipe(
      catchError((err: unknown) => throwError(() => this.toCheckoutError(err)))
    );
  }

  private toCheckoutError(err: unknown): CheckoutError {
    if (!(err instanceof HttpErrorResponse)) {
      return { kind: 'unknown', message: 'Unexpected error while placing the order.', status: 0 };
    }
    switch (err.status) {
      case 400:
        return {
          kind: 'validation',
          message: mapProblemDetails(err, 'The order request is invalid.'),
          status: 400
        };
      case 401:
        return {
          kind: 'unauthorized',
          message: 'Please sign in to complete your booking.',
          status: 401
        };
      case 404:
        return {
          kind: 'notFound',
          message: mapProblemDetails(err, 'The event or one of the ticket categories no longer exists.'),
          status: 404
        };
      case 409:
        return {
          kind: 'conflict',
          message: mapProblemDetails(
            err,
            'Not enough tickets are available anymore. Please review your selection.'
          ),
          status: 409
        };
      default:
        return {
          kind: 'unknown',
          message: mapProblemDetails(err, 'Failed to place the order.'),
          status: err.status
        };
    }
  }
}
