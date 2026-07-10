import { Injectable } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Appointment, PagedResult } from '../models/appointment.model';
import { Patient } from '../models/patient.model';

@Injectable({ providedIn: 'root' })
export class AppointmentService {
  constructor(private http: HttpClient) {}

  bookAppointment(dto: any): Observable<Appointment> {
    return this.http.post<Appointment>('/api/appointments/book', dto);
  }

  cancelAppointment(id: string): Observable<any> {
    return this.http.post<any>(`/api/appointments/cancel/${id}`, {});
  }

  getPatientDashboard(status?: string, page = 1, size = 10): Observable<PagedResult<Appointment>> {
    let params = new HttpParams().set('page', page.toString()).set('size', size.toString());
    if (status) {
      params = params.set('status', status);
    }
    return this.http.get<PagedResult<Appointment>>('/api/appointments/patient-dashboard', { params });
  }

  getAdminDoctorDashboard(filters: any, page = 1, size = 10): Observable<PagedResult<Appointment>> {
    let params = new HttpParams().set('page', page.toString()).set('size', size.toString());
    if (filters.status) params = params.set('status', filters.status);
    if (filters.startDate) params = params.set('startDate', filters.startDate);
    if (filters.endDate) params = params.set('endDate', filters.endDate);
    if (filters.search) params = params.set('search', filters.search);
    return this.http.get<PagedResult<Appointment>>('/api/appointments/admin-doctor-dashboard', { params });
  }

  getConsultedDoctors(): Observable<any[]> {
    return this.http.get<any[]>('/api/appointments/consulted-doctors');
  }

  getPatientsList(search?: string, page = 1, size = 10): Observable<PagedResult<Patient>> {
    let params = new HttpParams().set('page', page.toString()).set('size', size.toString());
    if (search) {
      params = params.set('search', search);
    }
    return this.http.get<PagedResult<Patient>>('/api/appointments/patients-list', { params });
  }

  getAvailableDoctors(): Observable<any[]> {
    return this.http.get<any[]>('/api/appointments/available-doctors');
  }

  getBookingDetails(doctorId: string, clinicId: string): Observable<any> {
    return this.http.get<any>(`/api/appointments/booking-details?doctorId=${doctorId}&clinicId=${clinicId}`);
  }

  getSpecializations(): Observable<any[]> {
    return this.http.get<any[]>('/api/appointments/specializations');
  }

  searchDoctors(state?: string, city?: string, specializationId?: string, nameSearch?: string): Observable<any[]> {
    let params = new HttpParams();
    if (state) {
      params = params.set('state', state);
    }
    if (city) {
      params = params.set('city', city);
    }
    if (specializationId) {
      params = params.set('specializationId', specializationId);
    }
    if (nameSearch) {
      params = params.set('name', nameSearch);
    }

    return this.http.get<any[]>('/api/appointments/search-doctors', { params });
  }

  getClinicsForDoctor(doctorId: string): Observable<any[]> {
    return this.http.get<any[]>(`/api/appointments/doctors/${doctorId}/clinics`);
  }

  getBookedSlots(doctorId: string, clinicId: string, date: string, patientId?: string): Observable<any[]> {
    let params = new HttpParams()
      .set('doctorId', doctorId)
      .set('clinicId', clinicId)
      .set('date', date);
    if (patientId) {
      params = params.set('patientId', patientId);
    }
    return this.http.get<any[]>('/api/appointments/booked-slots', { params });
  }
}
