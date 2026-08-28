import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Weather } from './weather';
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

  afterEach(() => httpMock.verify());

  it('does not call the API until the button is clicked', () => {
    const fixture = TestBed.createComponent(Weather);
    fixture.detectChanges();

    httpMock.expectNone(FORECAST_URL);
  });

  it('renders a row per forecast once loaded', async () => {
    const fixture = TestBed.createComponent(Weather);
    fixture.detectChanges();

    const button = fixture.nativeElement.querySelector('button') as HTMLButtonElement;
    button.click();

    httpMock.expectOne(FORECAST_URL).flush(sample);
    await fixture.whenStable();
    fixture.detectChanges();

    const rows = fixture.nativeElement.querySelectorAll('tbody tr');
    expect(rows.length).toBe(2);
    // A null summary should render as a dash rather than "null".
    expect(rows[1].textContent).toContain('—');
  });

  it('explains a status 0 failure as a likely CORS problem', async () => {
    const fixture = TestBed.createComponent(Weather);
    fixture.detectChanges();

    (fixture.nativeElement.querySelector('button') as HTMLButtonElement).click();

    httpMock
      .expectOne(FORECAST_URL)
      .error(new ProgressEvent('error'), { status: 0, statusText: 'Unknown Error' });
    await fixture.whenStable();
    fixture.detectChanges();

    const alert = fixture.nativeElement.querySelector('[role="alert"]') as HTMLElement;
    expect(alert.textContent).toContain('CORS');
  });
});
