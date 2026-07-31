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
    if (path.startsWith('/superadmin') || path.includes('/superadmin/')) return 'SuperAdmin';
    if (path.startsWith('/doctor') || path.includes('/doctor/')) return 'Doctor';
    if (path.startsWith('/admin') || path.includes('/admin/')) return 'Admin';
    if (path.startsWith('/patient/') || path === '/patient' || path.includes('/patient/')) return 'Patient';
    return null;
  }

  public getActiveUserForCurrentRoute(): any {
    const routeRole = this.getRoleFromPath();
    const storageKeys = routeRole
      ? [`healsync_auth_${routeRole}`]
      : ['healsync_auth_Doctor', 'healsync_auth_Patient', 'healsync_auth_Admin', 'healsync_auth_SuperAdmin'];

    // 1. Try tab-isolated sessionStorage first
    for (const key of storageKeys) {
      const raw = sessionStorage.getItem(key);
      if (raw) {
        try { return JSON.parse(raw); } catch { }
      }
    }

    // 2. Fallback to localStorage
    for (const key of storageKeys) {
      const raw = localStorage.getItem(key);
      if (raw) {
        try { return JSON.parse(raw); } catch { }
      }
    }

    return null;
  }

  private saveRoleSession(user: any): void {
    if (!user || !user.role) return;
    const key = `healsync_auth_${user.role}`;
    // Save to tab-specific sessionStorage and cross-tab localStorage
    sessionStorage.setItem(key, JSON.stringify(user));
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
      const key = `healsync_auth_${roleToLogout}`;
      sessionStorage.removeItem(key);
      localStorage.removeItem(key);
    } else {
      const roles = ['Patient', 'Doctor', 'Admin', 'SuperAdmin'];
      roles.forEach(r => {
        sessionStorage.removeItem(`healsync_auth_${r}`);
        localStorage.removeItem(`healsync_auth_${r}`);
      });
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
    const raw = sessionStorage.getItem(key) || localStorage.getItem(key);
    if (raw) {
      try {
        const user = JSON.parse(raw);
        user.firstName = firstName;
        sessionStorage.setItem(key, JSON.stringify(user));
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
    // Single Source of Truth: Extract role from cryptographic JWT token first
    const decoded = this.getDecodedToken(specificRole);
    if (decoded) {
      const jwtRole = decoded['role'] || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      if (jwtRole) return jwtRole;
    }

    const activeUser = this.getActiveUserForCurrentRoute();
    return activeUser?.role || null;
  }

  getToken(specificRole?: string): string | null {
    const activeUser = this.getActiveUserForCurrentRoute();
    if (activeUser && activeUser.token) {
      return activeUser.token;
    }
    const targetRole = specificRole || this.getRoleFromPath();
    if (targetRole) {
      const raw = sessionStorage.getItem(`healsync_auth_${targetRole}`) || localStorage.getItem(`healsync_auth_${targetRole}`);
      if (raw) {
        try {
          const parsed = JSON.parse(raw);
          return parsed.token || null;
        } catch { }
      }
    }
    return null;
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
