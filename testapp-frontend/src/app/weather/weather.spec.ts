import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { Weather } from './weather';
import { RETRY_COUNT, RETRY_DELAY_MS } from './weather-api';
import { APP_CONFIG } from '../core/app-config';
import { type WeatherForecast } from './weather-forecast';

const API_BASE_URL = 'http://api.test';
const FORECAST_URL = `${API_BASE_URL}/weatherforecast`;

const sample: WeatherForecast[] = [
  { date: '2026-08-29', temperatureC: 20, temperatureF: 68, summary: 'Mild' },
  { date: '2026-08-30', temperatureC: 30, temperatureF: 86, summary: null },
];

describe('Weather', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    // The app is zoneless, so Angular's fakeAsync/tick are unavailable.
    // rxjs timer() schedules on setTimeout, which vitest can drive directly.
    vi.useFakeTimers();

    await TestBed.configureTestingModule({
      imports: [Weather],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: APP_CONFIG, useValue: { apiBaseUrl: API_BASE_URL } },
      ],
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
    vi.useRealTimers();
  });

  function clickButton() {
    const fixture = TestBed.createComponent(Weather);
    fixture.detectChanges();
    (fixture.nativeElement.querySelector('button') as HTMLButtonElement).click();
    return fixture;
  }

  it('does not call the API until the button is clicked', () => {
    const fixture = TestBed.createComponent(Weather);
    fixture.detectChanges();

    httpMock.expectNone(FORECAST_URL);
  });

  it('renders a row per forecast once loaded', async () => {
    const fixture = clickButton();

    httpMock.expectOne(FORECAST_URL).flush(sample);
    await vi.advanceTimersByTimeAsync(0);
    fixture.detectChanges();

    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    // A null summary should render as a dash rather than "null".
    expect(rows[1].textContent).toContain('—');
  });

  it('retries a status 0 failure, then reports it without blaming CORS alone', async () => {
    const fixture = clickButton();

    // One initial attempt plus RETRY_COUNT retries, each after a growing delay.
    for (let attempt = 0; attempt <= RETRY_COUNT; attempt++) {
      httpMock.expectOne(FORECAST_URL).error(new ProgressEvent('error'), { status: 0 });
      await vi.advanceTimersByTimeAsync((attempt + 1) * RETRY_DELAY_MS);
    }

    fixture.detectChanges();

    const alert = fixture.nativeElement.querySelector('[role="alert"]') as HTMLElement;
    expect(alert.textContent).toContain('No response from the API');
    // The old message named CORS as the cause; it is only one possibility.
    expect(alert.textContent).toContain('zero replicas');
  });

  it('recovers when a retry succeeds', async () => {
    const fixture = clickButton();

    httpMock.expectOne(FORECAST_URL).error(new ProgressEvent('error'), { status: 0 });
    await vi.advanceTimersByTimeAsync(RETRY_DELAY_MS);

    httpMock.expectOne(FORECAST_URL).flush(sample);
    await vi.advanceTimersByTimeAsync(0);

    fixture.detectChanges();

    expect(fixture.nativeElement.querySelectorAll('tbody tr').length).toBe(2);
    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
  });

  it('does not retry a 4xx, since that is not transient', async () => {
    const fixture = clickButton();

    httpMock.expectOne(FORECAST_URL).flush('nope', { status: 404, statusText: 'Not Found' });
    await vi.advanceTimersByTimeAsync(RETRY_DELAY_MS * (RETRY_COUNT + 1));

    fixture.detectChanges();

    // No further attempt was made despite advancing past every retry delay.
    httpMock.expectNone(FORECAST_URL);
    const alert = fixture.nativeElement.querySelector('[role="alert"]') as HTMLElement;
    expect(alert.textContent).toContain('404');
  });
});
