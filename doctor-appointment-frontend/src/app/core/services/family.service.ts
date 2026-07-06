import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Patient } from '../models/patient.model';

@Injectable({ providedIn: 'root' })
export class FamilyService {
  constructor(private http: HttpClient) {}

  addFamilyMember(memberDto: any): Observable<{ verificationId: string; message: string }> {
    return this.http.post<{ verificationId: string; message: string }>('/api/family/add', memberDto);
  }

  verifyFamilyOtp(verificationId: string, otpCode: string): Observable<Patient> {
    return this.http.post<Patient>('/api/family/verify', { verificationId, otpCode });
  }

  getFamilyMembers(): Observable<Patient[]> {
    return this.http.get<Patient[]>('/api/family');
  }
}
