import { HttpContext, HttpContextToken } from '@angular/common/http';

/**
 * When `true`, the `authInterceptor` will NOT attach an `Authorization`
 * header to the request and will NOT trigger a `/login` redirect on 401.
 * Use this for public endpoints (e.g. login, register) invoked through
 * hand-written HttpClient calls.
 */
export const SKIP_AUTH = new HttpContextToken<boolean>(() => false);

/**
 * Per-request timeout in milliseconds enforced by `timeoutInterceptor`.
 * Defaults to 30_000 ms. Set a larger value for known-slow endpoints.
 */
export const REQUEST_TIMEOUT_MS = new HttpContextToken<number>(() => 30_000);

export function skipAuth(context: HttpContext = new HttpContext()): HttpContext {
  return context.set(SKIP_AUTH, true);
}

export function withTimeout(
  ms: number,
  context: HttpContext = new HttpContext()
): HttpContext {
  return context.set(REQUEST_TIMEOUT_MS, ms);
}
