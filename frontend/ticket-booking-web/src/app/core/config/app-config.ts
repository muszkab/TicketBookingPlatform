import { environment } from '../../../environments/environment';

/**
 * Runtime configuration, fetched from `config.json` before the application bootstraps.
 *
 * The SPA image is built once and deployed to several environments, so the API base path cannot be
 * baked into the bundle. Behind a same-origin proxy (Docker Compose/nginx) it stays empty and the
 * generated API client issues relative requests; when the SPA and the API live on different
 * domains (Static Web Apps + Container Apps) it holds the absolute API URL and the API must allow
 * that origin via `Cors:AllowedOrigins`.
 */
export interface AppConfig {
  apiBasePath: string;
}

let runtimeConfig: AppConfig = { apiBasePath: environment.apiBasePath };

export function getAppConfig(): AppConfig {
  return runtimeConfig;
}

export async function loadAppConfig(): Promise<AppConfig> {
  try {
    const response = await fetch('config.json', { cache: 'no-cache' });

    if (response.ok) {
      const config = (await response.json()) as Partial<AppConfig>;

      if (typeof config.apiBasePath === 'string') {
        runtimeConfig = { apiBasePath: config.apiBasePath };
      }
    }
  } catch {
    // Keep the compile-time default: a missing or malformed config.json must not break startup.
  }

  return runtimeConfig;
}
