import { Routes } from '@angular/router';

export const ORDERS_ROUTES: Routes = [
  {
    path: ':id/pay',
    loadComponent: () =>
      import('./order-payment.component').then((m) => m.OrderPaymentComponent)
  },
  {
    path: ':id',
    loadComponent: () =>
      import('./order-details.component').then((m) => m.OrderDetailsComponent)
  }
];
