import { InjectionToken } from '@angular/core';

/**
 * Configuration resolved at runtime, not at build time.
 *
 * The values are fetched from `/config.json` before the application
 * bootstraps, which keeps the API URL out of the compiled bundle. One image
 * can then run against any environment — the container rewrites config.json
 * on startup from the API_BASE_URL environment variable.
 */
export interface AppConfig {
  /** Origin of the TestApp API, with no trailing slash. */
  apiBaseUrl: string;
}

export const APP_CONFIG = new InjectionToken<AppConfig>('APP_CONFIG');
