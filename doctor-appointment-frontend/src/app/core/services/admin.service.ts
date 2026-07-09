import { Injectable } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class AdminService {
  constructor(private http: HttpClient) {}

  getPendingDoctors(): Observable<any[]> {
    return this.http.get<any[]>('/api/admin/pending-doctors');
  }

  verifyDoctor(doctorId: string, status: string): Observable<any> {
    return this.http.post<any>(`/api/admin/verify-doctor/${doctorId}?status=${status}`, {});
  }

  getPendingClinics(): Observable<any[]> {
    return this.http.get<any[]>('/api/clinics/pending');
  }

  getPendingAdmins(): Observable<any[]> {
    return this.http.get<any[]>('/api/clinics/pending-admins');
  }

  verifyClinic(clinicId: string): Observable<any> {
    return this.http.post<any>(`/api/clinics/verify-clinic/${clinicId}`, {});
  }

  verifyAdmin(adminId: string): Observable<any> {
    return this.http.post<any>(`/api/clinics/verify-admin/${adminId}`, {});
  }

  registerClinic(dto: any): Observable<any> {
    const userId = sessionStorage.getItem('userId') || '';
    const headers = new HttpHeaders().set('X-User-Id', userId);
    return this.http.post<any>('/api/clinics/register', dto, { headers });
  }

  registerClinicOnly(dto: any): Observable<any> {
    const userId = sessionStorage.getItem('userId') || '';
    const headers = new HttpHeaders().set('X-User-Id', userId);
    return this.http.post<any>('/api/clinics/register-only', dto, { headers });
  }

  registerClinicAdmin(dto: any): Observable<any> {
    const userId = sessionStorage.getItem('userId') || '';
    const headers = new HttpHeaders().set('X-User-Id', userId);
    return this.http.post<any>('/api/clinics/register-admin', dto, { headers });
  }

  getDoctorClinics(): Observable<any[]> {
    const userId = sessionStorage.getItem('userId') || '';
    const headers = new HttpHeaders().set('X-User-Id', userId);
    return this.http.get<any[]>('/api/clinics', { headers });
  }

  getDoctorAdmins(): Observable<any[]> {
    const userId = sessionStorage.getItem('userId') || '';
    const headers = new HttpHeaders().set('X-User-Id', userId);
    return this.http.get<any[]>('/api/clinics/admins', { headers });
  }

  getAllDoctors(search: string = '', status: string = ''): Observable<any[]> {
    return this.http.get<any[]>(`/api/admin/doctors?search=${search}&status=${status}`);
  }

  getAllClinics(search: string = '', state: string = '', city: string = '', isVerified?: any): Observable<any[]> {
    let url = `/api/admin/clinics?search=${search}&state=${state}&city=${city}`;
    if (isVerified !== undefined && isVerified !== null && isVerified !== '' && isVerified !== 'undefined' && isVerified !== 'null') {
      const boolVal = String(isVerified) === 'true';
      url += `&isVerified=${boolVal}`;
    }
    return this.http.get<any[]>(url);
  }

  getClinicHistory(clinicId: string): Observable<any[]> {
    return this.http.get<any[]>(`/api/clinics/${clinicId}/history`);
  }

  getAllAdmins(search: string = '', isVerified?: any): Observable<any[]> {
    let url = `/api/admin/admins?search=${search}`;
    if (isVerified !== undefined && isVerified !== null && isVerified !== '' && isVerified !== 'undefined' && isVerified !== 'null') {
      const boolVal = String(isVerified) === 'true';
      url += `&isVerified=${boolVal}`;
    }
    return this.http.get<any[]>(url);
  }

  approveDoctor(doctorUserId: string): Observable<any> {
    return this.verifyDoctor(doctorUserId, 'Verified');
  }

  rejectDoctor(doctorUserId: string): Observable<any> {
    return this.verifyDoctor(doctorUserId, 'Rejected');
  }

  rejectClinic(clinicId: string, reason: string): Observable<any> {
    return this.http.post<any>(`/api/clinics/verify-clinic/${clinicId}/reject`, { rejectionReason: reason });
  }

  rejectAdmin(adminId: string): Observable<any> {
    return this.http.post<any>(`/api/clinics/reject-admin/${adminId}`, {});
  }

  updateClinic(clinicId: string, dto: any): Observable<any> {
    const userId = sessionStorage.getItem('userId') || '';
    const headers = new HttpHeaders().set('X-User-Id', userId);
    return this.http.put<any>(`/api/clinics/${clinicId}`, dto, { headers });
  }

  getAdminClinic(): Observable<any> {
    const userId = sessionStorage.getItem('userId') || '';
    const headers = new HttpHeaders().set('X-User-Id', userId);
    return this.http.get<any>('/api/clinics/my-clinic', { headers });
  }

  updateClinicByAdmin(dto: any): Observable<any> {
    const userId = sessionStorage.getItem('userId') || '';
    const headers = new HttpHeaders().set('X-User-Id', userId);
    return this.http.put<any>('/api/clinics/admin-update', dto, { headers });
  }
}
