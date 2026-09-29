import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';
import { provideApi } from './api';
import { authInterceptor } from './core/auth/auth.interceptor';
import { timeoutInterceptor } from './core/http/timeout.interceptor';
import { getAppConfig } from './core/config/app-config';
import { routes } from './app.routes';

// NOTE: this must stay a function. As an `export const` it would be evaluated while the module is
// imported — before `loadAppConfig()` had a chance to run — and the API client would capture the
// compile-time default instead of the base path from config.json.
export function createAppConfig(): ApplicationConfig {
  return {
    providers: [
      provideBrowserGlobalErrorListeners(),
      provideRouter(routes),
      provideHttpClient(
        withFetch(),
        withInterceptors([authInterceptor, timeoutInterceptor])
      ),
      provideAnimationsAsync(),
      provideApi(getAppConfig().apiBasePath)
    ]
  };
}
