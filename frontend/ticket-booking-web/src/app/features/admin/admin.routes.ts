import { Routes } from '@angular/router';

export const ADMIN_ROUTES: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./admin-shell.component').then((m) => m.AdminShellComponent),
    children: [
      { path: '', pathMatch: 'full', redirectTo: 'events' },
      {
        path: 'events',
        loadComponent: () =>
          import('./events/admin-events-placeholder.component').then(
            (m) => m.AdminEventsPlaceholderComponent
          )
      }
    ]
  }
];
