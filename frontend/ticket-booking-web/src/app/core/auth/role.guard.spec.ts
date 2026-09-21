import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';

import { NotificationService } from '../notifications/notification.service';
import { AuthService } from './auth.service';
import { roleGuard } from './role.guard';
import { USER_ROLES } from './roles';

describe('roleGuard', () => {
  let authService: { isAuthenticated: ReturnType<typeof vi.fn>; roles: ReturnType<typeof vi.fn> };
  let router: { createUrlTree: ReturnType<typeof vi.fn> };
  let notifications: { error: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    authService = { isAuthenticated: vi.fn(), roles: vi.fn() };
    router = { createUrlTree: vi.fn().mockReturnValue('URL_TREE') };
    notifications = { error: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        { provide: AuthService, useValue: authService },
        { provide: Router, useValue: router },
        { provide: NotificationService, useValue: notifications }
      ]
    });
  });

  it('redirects to /login when the user is not authenticated', () => {
    authService.isAuthenticated.mockReturnValue(false);
    const guard = roleGuard([USER_ROLES.Admin]);

    const result = TestBed.runInInjectionContext(() =>
      guard({} as never, { url: '/admin' } as never)
    );

    expect(router.createUrlTree).toHaveBeenCalledWith(['/login'], {
      queryParams: { returnUrl: '/admin' }
    });
    expect(result).toBe('URL_TREE');
  });

  it('allows navigation when the user has one of the allowed roles', () => {
    authService.isAuthenticated.mockReturnValue(true);
    authService.roles.mockReturnValue([USER_ROLES.Admin]);
    const guard = roleGuard([USER_ROLES.Admin]);

    const result = TestBed.runInInjectionContext(() =>
      guard({} as never, { url: '/admin' } as never)
    );

    expect(result).toBe(true);
    expect(notifications.error).not.toHaveBeenCalled();
  });

  it('denies navigation and shows an error when the user lacks the required role', () => {
    authService.isAuthenticated.mockReturnValue(true);
    authService.roles.mockReturnValue([USER_ROLES.Customer]);
    const guard = roleGuard([USER_ROLES.Admin]);

    const result = TestBed.runInInjectionContext(() =>
      guard({} as never, { url: '/admin' } as never)
    );

    expect(notifications.error).toHaveBeenCalledWith(
      'You do not have permission to access this area.'
    );
    expect(router.createUrlTree).toHaveBeenCalledWith(['/events']);
    expect(result).toBe('URL_TREE');
  });
});
