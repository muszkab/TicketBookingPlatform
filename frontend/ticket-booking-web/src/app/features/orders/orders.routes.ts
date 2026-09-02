import { Routes } from '@angular/router';

export const ORDERS_ROUTES: Routes = [
  {
    path: ':id/confirmation',
    loadComponent: () =>
      import('./order-confirmation.component').then((m) => m.OrderConfirmationComponent)
  }
];
