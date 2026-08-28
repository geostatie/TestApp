import { bootstrapApplication } from '@angular/platform-browser';
import { appConfig } from './app/app.config';
import { App } from './app/app';
import { APP_CONFIG, type AppConfig } from './app/core/app-config';

/**
 * Load runtime configuration before bootstrapping.
 *
 * Doing this ahead of bootstrap (rather than in an initializer) means
 * APP_CONFIG is always available synchronously to every injector, so no
 * service has to guard against a half-initialised config.
 *
 * The relative URL matters: it resolves against <base href> so the app still
 * works if it is ever served from a sub-path.
 */
async function loadConfig(): Promise<AppConfig> {
  const response = await fetch('config.json', { cache: 'no-cache' });

  if (!response.ok) {
    throw new Error(`Could not load config.json (HTTP ${response.status}). The app cannot start without it.`);
  }

  return (await response.json()) as AppConfig;
}

loadConfig()
  .then((config) =>
    bootstrapApplication(App, {
      ...appConfig,
      providers: [...appConfig.providers, { provide: APP_CONFIG, useValue: config }],
    }),
  )
  .catch((err) => console.error(err));
