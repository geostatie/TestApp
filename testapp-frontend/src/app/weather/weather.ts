import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { WeatherApi } from './weather-api';
import { type WeatherForecast } from './weather-forecast';

@Component({
  selector: 'app-weather',
  templateUrl: './weather.html',
  styleUrl: './weather.scss',
})
export class Weather {
  readonly #api = inject(WeatherApi);

  protected readonly forecasts = signal<WeatherForecast[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);
  protected readonly hasLoaded = signal(false);

  protected load(): void {
    this.loading.set(true);
    this.error.set(null);

    this.#api.getForecast().subscribe({
      next: (forecasts) => {
        this.forecasts.set(forecasts);
        this.hasLoaded.set(true);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(describeError(err));
        this.loading.set(false);
      },
    });
  }
}

/**
 * Turn an HTTP failure into something actionable.
 *
 * Status 0 means no response was received at all, so the cause is genuinely
 * ambiguous. Naming only CORS here was actively misleading: with the API on
 * Container Apps at minReplicas 0, a dropped connection during scale-down or a
 * cold start produces exactly the same status. The request has already been
 * retried by WeatherApi before reaching this point.
 */
function describeError(err: HttpErrorResponse): string {
  if (err.status === 0) {
    return (
      'No response from the API after retrying. The cause cannot be determined from ' +
      'the browser alone — it may be starting up from zero replicas, a dropped ' +
      'connection, or a CORS rejection. The browser console shows which.'
    );
  }

  if (err.status >= 500) {
    return `The API failed with ${err.status} ${err.statusText || ''}`.trim();
  }

  return `The API rejected the request: ${err.status} ${err.statusText || ''}`.trim();
}
