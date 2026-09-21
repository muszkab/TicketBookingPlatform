import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of } from 'rxjs';

import { AuthService as GeneratedAuthService } from '../../api';
import { NotificationService } from '../notifications/notification.service';
import { AuthService } from './auth.service';
import { USER_ROLES } from './roles';

function base64UrlEncode(json: unknown): string {
  const base64 = btoa(JSON.stringify(json));
  return base64.replace(/\+/g, '-').replace(/\//g, '_').replace(/=+$/, '');
}

function makeToken(payload: Record<string, unknown>): string {
  const header = base64UrlEncode({ alg: 'none', typ: 'JWT' });
  const body = base64UrlEncode(payload);
  return `${header}.${body}.signature`;
}

describe('AuthService', () => {
  let generatedApi: { login: ReturnType<typeof vi.fn>; register: ReturnType<typeof vi.fn> };
  let router: { navigate: ReturnType<typeof vi.fn>; url: string };
  let notifications: { info: ReturnType<typeof vi.fn> };

  beforeEach(() => {
    localStorage.clear();
    vi.useRealTimers();

    generatedApi = {
      login: vi.fn(),
      register: vi.fn()
    };
    router = { navigate: vi.fn(), url: '/events' };
    notifications = { info: vi.fn() };

    TestBed.configureTestingModule({
      providers: [
        { provide: GeneratedAuthService, useValue: generatedApi },
        { provide: Router, useValue: router },
        { provide: NotificationService, useValue: notifications }
      ]
    });
  });

  afterEach(() => {
    localStorage.clear();
    vi.useRealTimers();
  });

  function futureExpiry(msFromNow: number): string {
    return new Date(Date.now() + msFromNow).toISOString();
  }

  it('starts unauthenticated with no stored session', () => {
    const service = TestBed.inject(AuthService);

    expect(service.isAuthenticated()).toBe(false);
    expect(service.accessToken()).toBeNull();
    expect(service.userName()).toBeNull();
  });

  it('login stores the session and marks the user as authenticated', () => {
    const token = makeToken({ sub: 'user-1', email: 'user@example.com' });
    generatedApi.login.mockReturnValue(
      of({ accessToken: token, expiresAt: futureExpiry(60_000) })
    );

    const service = TestBed.inject(AuthService);
    service.login({ email: 'user@example.com', password: 'secret' }).subscribe();

    expect(service.isAuthenticated()).toBe(true);
    expect(service.accessToken()).toBe(token);
    expect(service.userName()).toBe('user@example.com');
    expect(JSON.parse(localStorage.getItem('tbp.auth')!).accessToken).toBe(token);
  });

  it('exposes roles and isAdmin based on the decoded token claims', () => {
    const token = makeToken({ sub: 'user-2', role: [USER_ROLES.Admin, USER_ROLES.Customer] });
    generatedApi.login.mockReturnValue(
      of({ accessToken: token, expiresAt: futureExpiry(60_000) })
    );

    const service = TestBed.inject(AuthService);
    service.login({ email: 'admin@example.com', password: 'secret' }).subscribe();

    expect(service.roles()).toEqual([USER_ROLES.Admin, USER_ROLES.Customer]);
    expect(service.isAdmin()).toBe(true);
    expect(service.hasRole(USER_ROLES.Customer)).toBe(true);
    expect(service.hasRole(USER_ROLES.Organizer)).toBe(false);
  });

  it('logout clears the in-memory state and localStorage', () => {
    const token = makeToken({ sub: 'user-1' });
    generatedApi.login.mockReturnValue(
      of({ accessToken: token, expiresAt: futureExpiry(60_000) })
    );

    const service = TestBed.inject(AuthService);
    service.login({ email: 'user@example.com', password: 'secret' }).subscribe();
    expect(service.isAuthenticated()).toBe(true);

    service.logout();

    expect(service.isAuthenticated()).toBe(false);
    expect(localStorage.getItem('tbp.auth')).toBeNull();
  });

  it('treats an already-expired stored session as unauthenticated', () => {
    const token = makeToken({ sub: 'user-1' });
    localStorage.setItem(
      'tbp.auth',
      JSON.stringify({ accessToken: token, expiresAt: new Date(Date.now() - 1_000).toISOString() })
    );

    const service = TestBed.inject(AuthService);

    expect(service.isAuthenticated()).toBe(false);
    expect(service.accessToken()).toBeNull();
  });

  it('logs the user out and redirects to /login when the session expires', () => {
    vi.useFakeTimers();
    const token = makeToken({ sub: 'user-1' });
    generatedApi.login.mockReturnValue(
      of({ accessToken: token, expiresAt: futureExpiry(5_000) })
    );

    const service = TestBed.inject(AuthService);
    service.login({ email: 'user@example.com', password: 'secret' }).subscribe();
    expect(service.isAuthenticated()).toBe(true);

    vi.advanceTimersByTime(6_000);

    expect(service.isAuthenticated()).toBe(false);
    expect(notifications.info).toHaveBeenCalledWith(
      'Your session has expired. Please sign in again.'
    );
    expect(router.navigate).toHaveBeenCalledWith(['/login'], {
      queryParams: { returnUrl: '/events' }
    });
  });

  it('register delegates to the generated API client', () => {
    generatedApi.register.mockReturnValue(of({}));
    const service = TestBed.inject(AuthService);

    service.register({ email: 'new@example.com', password: 'secret' } as never).subscribe();

    expect(generatedApi.register).toHaveBeenCalledWith({
      email: 'new@example.com',
      password: 'secret'
    });
  });
});
