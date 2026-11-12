/** Mirrors ClinicSummary returned by GET /clinics. */
export interface Clinic {
  id: number;
  name: string;
  city: string;
  address: string;
  doctorCount: number;
}

/** Mirrors DoctorSummary returned by GET /clinics/{id}/doctors. */
export interface Doctor {
  id: number;
  name: string;
  specialty: string;
}
