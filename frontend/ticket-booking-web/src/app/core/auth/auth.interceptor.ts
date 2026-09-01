import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { SKIP_AUTH } from '../http/http-context-tokens';
import { AuthService } from './auth.service';

// Safety net for calls made through the generated API client, which
// cannot set HttpContext tokens. Hand-written HttpClient calls should
// prefer the `SKIP_AUTH` context token via `skipAuth()`.
const AUTH_FREE_PATHS = ['/api/v1/Auth/login', '/api/v1/Auth/register'];

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  const skipAuth =
    req.context.get(SKIP_AUTH) ||
    AUTH_FREE_PATHS.some((path) => req.url.includes(path));
  const token = auth.accessToken();

  const outgoing =
    !skipAuth && token && !req.headers.has('Authorization')
      ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : req;

  return next(outgoing).pipe(
    catchError((error: unknown) => {
      if (
        error instanceof HttpErrorResponse &&
        error.status === 401 &&
        !skipAuth &&
        !router.url.startsWith('/login')
      ) {
        auth.logout();
        router.navigate(['/login'], {
          queryParams: { returnUrl: router.url }
        });
      }
      return throwError(() => error);
    })
  );
};
