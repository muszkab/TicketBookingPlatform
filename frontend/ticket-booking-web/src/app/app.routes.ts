import { Routes } from '@angular/router';

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
  { path: '**', redirectTo: 'events' }
];
