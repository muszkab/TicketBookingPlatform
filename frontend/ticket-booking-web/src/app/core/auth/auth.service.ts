import { Injectable, computed, inject, signal } from '@angular/core';
import { Observable, tap } from 'rxjs';

import { AuthResultDto, AuthService as GeneratedAuthService, LoginRequest } from '../../api';
import { USER_ROLES } from './roles';

const STORAGE_KEY = 'tbp.auth';

interface StoredAuth {
  accessToken: string;
  expiresAt: string;
}

interface JwtPayload {
  sub?: string;
  email?: string;
  role?: string | string[];
  exp?: number;
  [claim: string]: unknown;
}

const ROLE_CLAIM_KEYS = [
  'role',
  'roles',
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'
];

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly api = inject(GeneratedAuthService);

  private readonly state = signal<StoredAuth | null>(this.readStorage());

  readonly accessToken = computed(() => {
    const value = this.state();
    return value && !this.isExpired(value) ? value.accessToken : null;
  });

  readonly isAuthenticated = computed(() => this.accessToken() !== null);

  readonly payload = computed<JwtPayload | null>(() => {
    const token = this.accessToken();
    return token ? this.decode(token) : null;
  });

  readonly userName = computed(() => {
    const p = this.payload();
    return p?.email ?? p?.sub ?? null;
  });

  readonly roles = computed<string[]>(() => {
    const p = this.payload();
    if (!p) {
      return [];
    }
    const collected: string[] = [];
    for (const key of ROLE_CLAIM_KEYS) {
      const value = p[key];
      if (typeof value === 'string') {
        collected.push(value);
      } else if (Array.isArray(value)) {
        for (const entry of value) {
          if (typeof entry === 'string') {
            collected.push(entry);
          }
        }
      }
    }
    return Array.from(new Set(collected));
  });

  readonly isAdmin = computed(() => this.roles().includes(USER_ROLES.Admin));

  hasRole(role: string): boolean {
    return this.roles().includes(role);
  }

  login(request: LoginRequest): Observable<AuthResultDto> {
    return this.api.login(request).pipe(tap((result) => this.setSession(result)));
  }

  logout(): void {
    this.state.set(null);
    try {
      localStorage.removeItem(STORAGE_KEY);
    } catch {
      // ignore storage errors (private mode / SSR)
    }
  }

  private setSession(result: AuthResultDto): void {
    const value: StoredAuth = {
      accessToken: result.accessToken,
      expiresAt: result.expiresAt
    };
    this.state.set(value);
    try {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(value));
    } catch {
      // ignore storage errors
    }
  }

  private readStorage(): StoredAuth | null {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      if (!raw) {
        return null;
      }
      const parsed = JSON.parse(raw) as StoredAuth;
      if (!parsed?.accessToken || this.isExpired(parsed)) {
        localStorage.removeItem(STORAGE_KEY);
        return null;
      }
      return parsed;
    } catch {
      return null;
    }
  }

  private isExpired(value: StoredAuth): boolean {
    const expiresAt = Date.parse(value.expiresAt);
    return Number.isFinite(expiresAt) && expiresAt <= Date.now();
  }

  private decode(token: string): JwtPayload | null {
    const parts = token.split('.');
    if (parts.length < 2) {
      return null;
    }
    try {
      const padded = parts[1].replace(/-/g, '+').replace(/_/g, '/');
      const json = atob(padded + '==='.slice((padded.length + 3) % 4));
      return JSON.parse(json) as JwtPayload;
    } catch {
      return null;
    }
  }
}
