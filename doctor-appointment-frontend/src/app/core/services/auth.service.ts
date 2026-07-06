import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private currentUserSubject = new BehaviorSubject<any>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    const cachedUser = sessionStorage.getItem('userId');
    if (cachedUser) {
      this.currentUserSubject.next({
        userId: cachedUser,
        email: sessionStorage.getItem('email'),
        role: sessionStorage.getItem('role'),
        token: sessionStorage.getItem('token'),
        firstName: sessionStorage.getItem('firstName'),
        lastName: sessionStorage.getItem('lastName'),
        profileId: sessionStorage.getItem('profileId')
      });
    }
  }

  register(registerDto: any): Observable<any> {
    return this.http.post<any>('/api/auth/register', registerDto);
  }

  registerDoctor(doctorRegisterDto: any): Observable<any> {
    return this.http.post<any>('/api/auth/register-doctor', doctorRegisterDto);
  }

  login(credentials: any): Observable<any> {
    return this.http.post<any>('/api/auth/login', credentials).pipe(
      tap(user => {
        sessionStorage.setItem('token', user.token);
        sessionStorage.setItem('userId', user.userId);
        sessionStorage.setItem('email', user.email);
        sessionStorage.setItem('role', user.role);
        sessionStorage.setItem('firstName', user.firstName || 'User');
        sessionStorage.setItem('lastName', user.lastName || '');
        sessionStorage.setItem('profileId', user.profileId || '');
        this.currentUserSubject.next(user);
      })
    );
  }

  verifyEmail(dto: { email: string; otp: string }): Observable<any> {
    return this.http.post<any>('/api/auth/verify-email', dto).pipe(
      tap(user => {
        sessionStorage.setItem('token', user.token);
        sessionStorage.setItem('userId', user.userId);
        sessionStorage.setItem('email', user.email);
        sessionStorage.setItem('role', user.role);
        sessionStorage.setItem('firstName', user.firstName || 'User');
        sessionStorage.setItem('lastName', user.lastName || '');
        sessionStorage.setItem('profileId', user.profileId || '');
        this.currentUserSubject.next(user);
      })
    );
  }

  logout() {
    sessionStorage.clear();
    this.currentUserSubject.next(null);
  }

  getUserId(): string | null {
    return sessionStorage.getItem('userId');
  }

  getRole(): string | null {
    return sessionStorage.getItem('role');
  }

  getToken(): string | null {
    return sessionStorage.getItem('token');
  }

  isAuthenticated(): boolean {
    return !!this.getToken();
  }
}
