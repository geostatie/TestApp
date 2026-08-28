import { HttpClient } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '../core/app-config';
import { type WeatherForecast } from './weather-forecast';

@Service()
export class WeatherApi {
  readonly #http = inject(HttpClient);
  readonly #config = inject(APP_CONFIG);

  getForecast(): Observable<WeatherForecast[]> {
    return this.#http.get<WeatherForecast[]>(`${this.#config.apiBaseUrl}/weatherforecast`);
  }
}
