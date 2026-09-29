import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideHttpClient, withFetch, withInterceptors } from '@angular/common/http';
import { provideAnimationsAsync } from '@angular/platform-browser/animations/async';
import { provideRouter } from '@angular/router';
import { provideApi } from './api';
import { authInterceptor } from './core/auth/auth.interceptor';
import { timeoutInterceptor } from './core/http/timeout.interceptor';
import { getAppConfig } from './core/config/app-config';
import { routes } from './app.routes';

export const appConfig: ApplicationConfig = {
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
