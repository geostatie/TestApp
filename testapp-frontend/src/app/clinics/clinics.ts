import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, inject, signal } from '@angular/core';
import { Subscription } from 'rxjs';
import { describeError } from '../core/describe-error';
import { ClinicsApi } from './clinics-api';
import { type Clinic, type Doctor } from './clinic';

@Component({
  selector: 'app-clinics',
  templateUrl: './clinics.html',
  styleUrl: './clinics.scss',
})
export class Clinics implements OnInit {
  readonly #api = inject(ClinicsApi);
  #doctorsRequest?: Subscription;

  protected readonly clinics = signal<Clinic[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected readonly selected = signal<Clinic | null>(null);
  protected readonly doctors = signal<Doctor[]>([]);
  protected readonly doctorsLoading = signal(false);
  protected readonly doctorsError = signal<string | null>(null);

  ngOnInit(): void {
    this.loadClinics();
  }

  protected loadClinics(): void {
    this.loading.set(true);
    this.error.set(null);

    this.#api.getClinics().subscribe({
      next: (clinics) => {
        this.clinics.set(clinics);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.error.set(describeError(err));
        this.loading.set(false);
      },
    });
  }

  protected select(clinic: Clinic): void {
    // A slow answer for the previous clinic must not overwrite this one.
    this.#doctorsRequest?.unsubscribe();

    this.selected.set(clinic);
    this.doctors.set([]);
    this.doctorsLoading.set(true);
    this.doctorsError.set(null);

    this.#doctorsRequest = this.#api.getDoctors(clinic.id).subscribe({
      next: (doctors) => {
        this.doctors.set(doctors);
        this.doctorsLoading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.doctorsError.set(describeError(err));
        this.doctorsLoading.set(false);
      },
    });
  }
}
