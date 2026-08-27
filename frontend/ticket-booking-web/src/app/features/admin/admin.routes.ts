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
          import('./events/admin-events-list.component').then(
            (m) => m.AdminEventsListComponent
          )
      },
      {
        path: 'events/new',
        loadComponent: () =>
          import('./events/admin-event-create.component').then(
            (m) => m.AdminEventCreateComponent
          )
      },
      {
        path: 'events/:id/edit',
        loadComponent: () =>
          import('./events/admin-event-edit.component').then(
            (m) => m.AdminEventEditComponent
          )
      }
    ]
  }
];
