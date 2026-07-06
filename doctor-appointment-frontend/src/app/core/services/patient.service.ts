import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Patient } from '../models/patient.model';

@Injectable({ providedIn: 'root' })
export class PatientService {
  constructor(private http: HttpClient) {}

  getPatientProfile(id: string): Observable<Patient> {
    return this.http.get<Patient>(`/api/patients/${id}`);
  }

  updatePatientProfile(id: string, profileDto: any): Observable<Patient> {
    return this.http.put<Patient>(`/api/patients/${id}`, profileDto);
  }

  getDoctorProfile(): Observable<any> {
    return this.http.get<any>('/api/users/doctor-profile');
  }

  updateDoctorProfile(profileDto: any): Observable<any> {
    return this.http.put<any>('/api/users/doctor-profile', profileDto);
  }

  getAdminProfile(): Observable<any> {
    return this.http.get<any>('/api/users/admin-profile');
  }

  updateAdminProfile(profileDto: any): Observable<any> {
    return this.http.put<any>('/api/users/admin-profile', profileDto);
  }

  getDoctorsDirectory(params: any): Observable<any> {
    return this.http.get<any>('/api/patients/doctors', { params });
  }
}
