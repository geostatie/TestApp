/** Mirrors the WeatherForecast record returned by the TestApp API. */
export interface WeatherForecast {
  /** ISO date, e.g. "2026-08-29". Serialized from a .NET DateOnly. */
  date: string;
  temperatureC: number;
  temperatureF: number;
  summary: string | null;
}
