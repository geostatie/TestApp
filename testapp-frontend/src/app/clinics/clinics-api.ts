import { HttpClient } from '@angular/common/http';
import { Service, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { APP_CONFIG } from '../core/app-config';
import { retryTransient } from '../core/http-retry';
import { type Clinic, type Doctor } from './clinic';

@Service()
export class ClinicsApi {
  readonly #http = inject(HttpClient);
  readonly #config = inject(APP_CONFIG);

  getClinics(): Observable<Clinic[]> {
    return this.#http.get<Clinic[]>(`${this.#config.apiBaseUrl}/clinics`).pipe(retryTransient());
  }

  getDoctors(clinicId: number): Observable<Doctor[]> {
    return this.#http
      .get<Doctor[]>(`${this.#config.apiBaseUrl}/clinics/${clinicId}/doctors`)
      .pipe(retryTransient());
  }
}
