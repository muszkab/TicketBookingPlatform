import { Routes } from '@angular/router';

export const ORDERS_ROUTES: Routes = [
  {
    path: ':id/pay',
    loadComponent: () =>
      import('./order-payment.component').then((m) => m.OrderPaymentComponent)
  },
  {
    path: ':id/confirmation',
    loadComponent: () =>
      import('./order-confirmation.component').then((m) => m.OrderConfirmationComponent)
  }
];
