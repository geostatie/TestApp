import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { afterEach, beforeEach, describe, expect, it, vi } from 'vitest';
import { Clinics } from './clinics';
import { RETRY_COUNT, RETRY_DELAY_MS } from '../core/http-retry';
import { APP_CONFIG } from '../core/app-config';
import { type Clinic, type Doctor } from './clinic';

const API_BASE_URL = 'http://api.test';
const CLINICS_URL = `${API_BASE_URL}/clinics`;
const doctorsUrl = (clinicId: number) => `${CLINICS_URL}/${clinicId}/doctors`;

const clinics: Clinic[] = [
  { id: 1, name: 'Aurora Medical Center', city: 'Bucharest', address: 'Str. Aviatorilor 21', doctorCount: 2 },
  { id: 2, name: 'Bega Medical', city: 'Timișoara', address: 'Str. Gheorghe Lazăr 15', doctorCount: 1 },
];

const doctors: Doctor[] = [
  { id: 1, name: 'Dr. Ana Ionescu', specialty: 'Cardiology' },
  { id: 2, name: 'Dr. Mihai Stan', specialty: 'Dermatology' },
];

describe('Clinics', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    // The app is zoneless, so Angular's fakeAsync/tick are unavailable.
    // rxjs timer() schedules on setTimeout, which vitest can drive directly.
    vi.useFakeTimers();

    await TestBed.configureTestingModule({
      imports: [Clinics],
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

  function render() {
    const fixture = TestBed.createComponent(Clinics);
    fixture.detectChanges();
    return fixture;
  }

  async function settle(fixture: ComponentFixture<Clinics>) {
    await vi.advanceTimersByTimeAsync(0);
    fixture.detectChanges();
  }

  it('loads the clinics on start and renders one card per clinic', async () => {
    const fixture = render();

    httpMock.expectOne(CLINICS_URL).flush(clinics);
    await settle(fixture);

    const cards = fixture.nativeElement.querySelectorAll('.clinics__card');
    expect(cards.length).toBe(2);
    expect(cards[0].textContent).toContain('Aurora Medical Center');
    expect(cards[1].textContent).toContain('1 doctor');
  });

  it('loads and lists the doctors of the selected clinic', async () => {
    const fixture = render();
    httpMock.expectOne(CLINICS_URL).flush(clinics);
    await settle(fixture);

    (fixture.nativeElement.querySelector('.clinics__card') as HTMLButtonElement).click();
    httpMock.expectOne(doctorsUrl(1)).flush(doctors);
    await settle(fixture);

    const card = fixture.nativeElement.querySelector('.clinics__card') as HTMLElement;
    expect(card.getAttribute('aria-pressed')).toBe('true');
    expect(fixture.nativeElement.querySelector('h3').textContent).toContain('Aurora Medical Center');
    expect(fixture.nativeElement.querySelectorAll('tbody tr').length).toBe(2);
  });

  it('ignores a late answer for a clinic that is no longer selected', async () => {
    const fixture = render();
    httpMock.expectOne(CLINICS_URL).flush(clinics);
    await settle(fixture);

    const cards = fixture.nativeElement.querySelectorAll('.clinics__card');
    (cards[0] as HTMLButtonElement).click();
    const firstRequest = httpMock.expectOne(doctorsUrl(1));
    (cards[1] as HTMLButtonElement).click();
    httpMock.expectOne(doctorsUrl(2)).flush([doctors[0]]);
    await settle(fixture);

    // The first request was cancelled when the second clinic was chosen.
    expect(firstRequest.cancelled).toBe(true);
    expect(fixture.nativeElement.querySelectorAll('tbody tr').length).toBe(1);
  });

  it('retries a status 0 failure, then reports it and offers to try again', async () => {
    const fixture = render();

    // One initial attempt plus RETRY_COUNT retries, each after a growing delay.
    for (let attempt = 0; attempt <= RETRY_COUNT; attempt++) {
      httpMock.expectOne(CLINICS_URL).error(new ProgressEvent('error'), { status: 0 });
      await vi.advanceTimersByTimeAsync((attempt + 1) * RETRY_DELAY_MS);
    }
    fixture.detectChanges();

    const alert = fixture.nativeElement.querySelector('[role="alert"]') as HTMLElement;
    expect(alert.textContent).toContain('No response from the API');
    expect(alert.textContent).toContain('zero replicas');

    (alert.querySelector('button') as HTMLButtonElement).click();
    httpMock.expectOne(CLINICS_URL).flush(clinics);
    await settle(fixture);

    expect(fixture.nativeElement.querySelector('[role="alert"]')).toBeNull();
    expect(fixture.nativeElement.querySelectorAll('.clinics__card').length).toBe(2);
  });

  it('does not retry a 4xx, since that is not transient', async () => {
    const fixture = render();

    httpMock.expectOne(CLINICS_URL).flush('nope', { status: 404, statusText: 'Not Found' });
    await vi.advanceTimersByTimeAsync(RETRY_DELAY_MS * (RETRY_COUNT + 1));
    fixture.detectChanges();

    // No further attempt was made despite advancing past every retry delay.
    httpMock.expectNone(CLINICS_URL);
    const alert = fixture.nativeElement.querySelector('[role="alert"]') as HTMLElement;
    expect(alert.textContent).toContain('404');
  });
});
