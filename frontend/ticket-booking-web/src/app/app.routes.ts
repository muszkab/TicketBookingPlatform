import { Routes } from '@angular/router';

import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';
import { USER_ROLES } from './core/auth/roles';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'events' },
  {
    path: 'events',
    loadChildren: () =>
      import('./features/events/events.routes').then((m) => m.EVENTS_ROUTES)
  },
  {
    path: 'login',
    loadComponent: () =>
      import('./features/auth/login.component').then((m) => m.LoginComponent)
  },
  {
    path: 'register',
    loadComponent: () =>
      import('./features/auth/register.component').then((m) => m.RegisterComponent)
  },
  {
    path: 'orders',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./features/orders/orders.routes').then((m) => m.ORDERS_ROUTES)
  },
  {
    path: 'tickets',
    canActivate: [authGuard],
    loadChildren: () =>
      import('./features/tickets/tickets.routes').then((m) => m.TICKETS_ROUTES)
  },
  {
    path: 'my-orders',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/orders/my-orders.component').then((m) => m.MyOrdersComponent)
  },
  {
    path: 'my-tickets',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/tickets/my-tickets.component').then((m) => m.MyTicketsComponent)
  },
  {
    path: 'checkout',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/checkout/checkout.component').then((m) => m.CheckoutComponent)
  },
  {
    path: 'admin',
    canActivate: [authGuard, roleGuard([USER_ROLES.Admin])],
    loadChildren: () =>
      import('./features/admin/admin.routes').then((m) => m.ADMIN_ROUTES)
  },
  { path: '**', redirectTo: 'events' }
];
