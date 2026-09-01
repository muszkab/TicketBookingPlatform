import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { TimeoutError, throwError, timeout } from 'rxjs';
import { catchError } from 'rxjs/operators';

import { REQUEST_TIMEOUT_MS } from './http-context-tokens';

/**
 * Enforces a per-request timeout. The threshold is read from the
 * `REQUEST_TIMEOUT_MS` HttpContext token (default: 30_000 ms).
 *
 * On timeout, the underlying `TimeoutError` is translated into an
 * `HttpErrorResponse` with `status: 0` so downstream error handling
 * (existing `mapError` helpers) can treat it uniformly as a network
 * failure ("Cannot reach the server.").
 */
export const timeoutInterceptor: HttpInterceptorFn = (req, next) => {
  const ms = req.context.get(REQUEST_TIMEOUT_MS);
  if (!ms || ms <= 0) {
    return next(req);
  }

  return next(req).pipe(
    timeout({ each: ms }),
    catchError((error: unknown) => {
      if (error instanceof TimeoutError) {
        return throwError(
          () =>
            new HttpErrorResponse({
              error: new Error(`Request timed out after ${ms} ms`),
              status: 0,
              statusText: 'Request Timeout',
              url: req.url
            })
        );
      }
      return throwError(() => error);
    })
  );
};
