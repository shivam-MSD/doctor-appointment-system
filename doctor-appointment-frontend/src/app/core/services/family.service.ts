import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class FamilyService {
  constructor(private http: HttpClient) {}

  getFamilyMembers(): Observable<any[]> {
    return this.http.get<any[]>('/api/patients/family');
  }

  addDependent(dto: any): Observable<any> {
    return this.http.post<any>('/api/patients/family/dependent', dto);
  }

  sendFamilyOtp(dto: any): Observable<any> {
    return this.http.post<any>('/api/patients/family/send-otp', dto);
  }

  verifyFamilyOtp(dto: any): Observable<any> {
    return this.http.post<any>('/api/patients/family/verify-otp', dto);
  }

  deleteFamilyMember(id: string): Observable<any> {
    return this.http.delete<any>(`/api/patients/family/${id}`);
  }
}
