import { Routes } from '@angular/router';

export const TICKETS_ROUTES: Routes = [
  {
    path: ':id',
    loadComponent: () =>
      import('./ticket-details.component').then((m) => m.TicketDetailsComponent)
  }
];
