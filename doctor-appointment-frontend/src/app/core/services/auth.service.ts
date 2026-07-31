import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private currentUserSubject = new BehaviorSubject<any>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(private http: HttpClient) {
    const activeUser = this.getActiveUserForCurrentRoute();
    if (activeUser) {
      this.currentUserSubject.next(activeUser);
    }

    // Real-time cross-tab session synchronization (Scenario 2B)
    window.addEventListener('storage', (event: StorageEvent) => {
      if (event.key && event.key.startsWith('healsync_auth_')) {
        const updatedUser = this.getActiveUserForCurrentRoute();
        this.currentUserSubject.next(updatedUser);
      }
    });
  }

  private getRoleFromPath(): string | null {
    const path = window.location.pathname.toLowerCase();
    if (path.includes('/patient')) return 'Patient';
    if (path.includes('/doctor')) return 'Doctor';
    if (path.includes('/admin')) return 'Admin';
    if (path.includes('/superadmin')) return 'SuperAdmin';
    return null;
  }

  public getActiveUserForCurrentRoute(): any {
    const routeRole = this.getRoleFromPath();
    if (routeRole) {
      const raw = localStorage.getItem(`healsync_auth_${routeRole}`);
      if (raw) {
        try { return JSON.parse(raw); } catch { }
      }
      return null;
    }

    // On generic public routes (like / or /home), check all stored role sessions
    const roles = ['Patient', 'Doctor', 'Admin', 'SuperAdmin'];
    for (const r of roles) {
      const raw = localStorage.getItem(`healsync_auth_${r}`);
      if (raw) {
        try { return JSON.parse(raw); } catch { }
      }
    }
    return null;
  }

  private saveRoleSession(user: any): void {
    if (!user || !user.role) return;
    const key = `healsync_auth_${user.role}`;
    localStorage.setItem(key, JSON.stringify(user));
    this.currentUserSubject.next(user);
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
        this.saveRoleSession(user);
      })
    );
  }

  verifyEmail(dto: { email: string; otp: string }): Observable<any> {
    return this.http.post<any>('/api/auth/verify-email', dto).pipe(
      tap(user => {
        this.saveRoleSession(user);
      })
    );
  }

  logout(specificRole?: string): void {
    const roleToLogout = specificRole || this.getRole() || this.getRoleFromPath();
    if (roleToLogout) {
      localStorage.removeItem(`healsync_auth_${roleToLogout}`);
    } else {
      // Fallback: clear all healsync role keys selectively
      const roles = ['Patient', 'Doctor', 'Admin', 'SuperAdmin'];
      roles.forEach(r => localStorage.removeItem(`healsync_auth_${r}`));
    }
    
    const remainingUser = this.getActiveUserForCurrentRoute();
    this.currentUserSubject.next(remainingUser);
  }

  private getDecodedToken(specificRole?: string): any {
    const token = this.getToken(specificRole);
    if (!token) return null;
    try {
      const payload = token.split('.')[1];
      const decodedJson = atob(payload.replace(/-/g, '+').replace(/_/g, '/'));
      return JSON.parse(decodedJson);
    } catch {
      return null;
    }
  }

  getFirstName(specificRole?: string): string {
    const activeUser = this.getActiveUserForCurrentRoute();
    if (activeUser && activeUser.firstName) {
      return activeUser.firstName;
    }
    const decoded = this.getDecodedToken(specificRole);
    if (decoded && (decoded.firstName || decoded.given_name || decoded.name)) {
      return decoded.firstName || decoded.given_name || decoded.name;
    }
    return activeUser?.email ? activeUser.email.split('@')[0] : '';
  }

  public updateCachedFirstName(firstName: string, role?: string): void {
    const activeRole = role || this.getRole(role) || this.getRoleFromPath();
    if (!activeRole) return;
    const key = `healsync_auth_${activeRole}`;
    const raw = localStorage.getItem(key);
    if (raw) {
      try {
        const user = JSON.parse(raw);
        user.firstName = firstName;
        localStorage.setItem(key, JSON.stringify(user));
        this.currentUserSubject.next(user);
      } catch { }
    }
  }

  getUserId(specificRole?: string): string | null {
    const activeUser = this.getActiveUserForCurrentRoute();
    const decoded = this.getDecodedToken(specificRole);
    if (decoded) {
      return decoded['nameid'] || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/nameidentifier'] || decoded['sub'] || activeUser?.userId;
    }
    return activeUser?.userId || null;
  }

  getRole(specificRole?: string): string | null {
    const targetRole = specificRole || this.getRoleFromPath();
    if (targetRole) {
      const raw = localStorage.getItem(`healsync_auth_${targetRole}`);
      if (raw) {
        try {
          const parsed = JSON.parse(raw);
          return parsed.role || targetRole;
        } catch { }
      }
      return null;
    }
    const activeUser = this.getActiveUserForCurrentRoute();
    return activeUser?.role || null;
  }

  getToken(specificRole?: string): string | null {
    const targetRole = specificRole || this.getRoleFromPath();
    if (targetRole) {
      const raw = localStorage.getItem(`healsync_auth_${targetRole}`);
      if (raw) {
        try {
          const parsed = JSON.parse(raw);
          return parsed.token || null;
        } catch { }
      }
      return null;
    }
    const activeUser = this.getActiveUserForCurrentRoute();
    return activeUser?.token || null;
  }

  isAuthenticated(specificRole?: string): boolean {
    const token = this.getToken(specificRole);
    if (!token) return false;
    const decoded = this.getDecodedToken(specificRole);
    if (!decoded) return false;
    if (decoded.exp) {
      return decoded.exp * 1000 > Date.now();
    }
    return true;
  }

  checkEmail(email: string, role?: string): Observable<any> {
    return this.http.post<any>('/api/auth/check-email', { email, role });
  }

  forgotPassword(email: string, role: string): Observable<any> {
    return this.http.post<any>('/api/auth/forgot-password', { email, role });
  }

  resetPassword(email: string, otp: string, newPassword: string, role: string): Observable<any> {
    return this.http.post<any>('/api/auth/reset-password', { email, otp, newPassword, role });
  }

  initiatePasswordUpdate(currentPassword: string): Observable<any> {
    return this.http.post<any>('/api/auth/initiate-password-update', { currentPassword });
  }

  updatePassword(otp: string, newPassword: string): Observable<any> {
    return this.http.post<any>('/api/auth/update-password', { otp, newPassword });
  }
}
