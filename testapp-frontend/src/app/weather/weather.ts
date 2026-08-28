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
 * Status 0 is the important case: the browser blocked the response before any
 * status was read. That is nearly always a CORS rejection or an unreachable
 * API, and the generic "Http failure response" text sends people hunting in
 * the wrong place.
 */
function describeError(err: HttpErrorResponse): string {
  if (err.status === 0) {
    return (
      'Could not reach the API. This is usually a CORS rejection or the API being ' +
      'unreachable — open the browser console for the specific CORS message.'
    );
  }

  return `The API responded with ${err.status} ${err.statusText || ''}`.trim();
}
