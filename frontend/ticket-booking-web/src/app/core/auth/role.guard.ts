import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { NotificationService } from '../notifications/notification.service';
import { AuthService } from './auth.service';

export function roleGuard(allowedRoles: readonly string[]): CanActivateFn {
  return (_route, state) => {
    const auth = inject(AuthService);
    const router = inject(Router);
    const notifications = inject(NotificationService);

    if (!auth.isAuthenticated()) {
      return router.createUrlTree(['/login'], {
        queryParams: { returnUrl: state.url }
      });
    }

    const roles = auth.roles();
    if (allowedRoles.some((role) => roles.includes(role))) {
      return true;
    }

    notifications.error('You do not have permission to access this area.');
    return router.createUrlTree(['/events']);
  };
}
