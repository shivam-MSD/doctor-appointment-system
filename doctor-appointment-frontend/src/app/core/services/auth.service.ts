import { Injectable, Injector } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, BehaviorSubject } from 'rxjs';
import { tap } from 'rxjs/operators';
import { NotificationService } from './notification.service';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private currentUserSubject = new BehaviorSubject<any>(null);
  public currentUser$ = this.currentUserSubject.asObservable();

  constructor(
    private http: HttpClient,
    private injector: Injector
  ) {
    const activeUser = this.getAnyActiveUser();
    if (activeUser) {
      this.currentUserSubject.next(activeUser);
    }

    // Real-time cross-tab session synchronization: Auto-login / Logout sync
    window.addEventListener('storage', (event: StorageEvent) => {
      if (event.key === 'healsync_logout_event' || (event.key && event.key.startsWith('healsync_auth_') && !event.newValue)) {
        this.logout();
      } else if (event.key && event.key.startsWith('healsync_auth_') && event.newValue) {
        try {
          sessionStorage.setItem(event.key, event.newValue);
          const user = JSON.parse(event.newValue);
          this.currentUserSubject.next(user);
        } catch { }
      }
    });
  }

  public getRoleFromPath(): string | null {
    const path = window.location.pathname.toLowerCase();
    if (path.startsWith('/superadmin') || path.includes('/superadmin/')) return 'SuperAdmin';
    if (path.startsWith('/doctor') || path.includes('/doctor/')) return 'Doctor';
    if (path.startsWith('/admin') || path.includes('/admin/')) return 'Admin';
    if (path.startsWith('/patient/') || path === '/patient' || path.includes('/patient/')) return 'Patient';
    return null;
  }

  public isLoggedIn(): boolean {
    return !!this.getAnyActiveUser();
  }

  /**
   * Checks if ANY valid active user (Patient, Doctor, Admin, SuperAdmin) exists across sessionStorage or localStorage.
   */
  public getAnyActiveUser(): any {
    const roles = ['Patient', 'Doctor', 'Admin', 'SuperAdmin'];
    
    // 1. Check current route role first
    const routeRole = this.getRoleFromPath();
    if (routeRole) {
      const key = `healsync_auth_${routeRole}`;
      const rawSession = sessionStorage.getItem(key);
      if (rawSession) {
        try {
          const user = JSON.parse(rawSession);
          if (user && user.token) return user;
        } catch { }
      }
      const rawLocal = localStorage.getItem(key);
      if (rawLocal) {
        try {
          const user = JSON.parse(rawLocal);
          if (user && user.token) {
            sessionStorage.setItem(key, rawLocal);
            return user;
          }
        } catch { }
      }
    }

    // 2. Check all roles across sessionStorage and localStorage
    for (const r of roles) {
      const key = `healsync_auth_${r}`;
      const rawSession = sessionStorage.getItem(key);
      if (rawSession) {
        try {
          const user = JSON.parse(rawSession);
          if (user && user.token) return user;
        } catch { }
      }
      const rawLocal = localStorage.getItem(key);
      if (rawLocal) {
        try {
          const user = JSON.parse(rawLocal);
          if (user && user.token) {
            sessionStorage.setItem(key, rawLocal);
            return user;
          }
        } catch { }
      }
    }

    return null;
  }

  public getActiveUserForCurrentRoute(): any {
    return this.getAnyActiveUser();
  }

  private saveRoleSession(user: any): void {
    if (!user || !user.role) return;
    const key = `healsync_auth_${user.role}`;
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

  /**
   * Enterprise-Grade Comprehensive Logout Sequence:
   * 1. Disconnect SignalR notifications BEFORE clearing storage (sending graceful disconnect frame with JWT).
   * 2. Clear sessionStorage completely.
   * 3. Targeted purge of HealSync-prefixed keys in localStorage.
   * 4. Broadcast explicit 'healsync_logout_event' timestamp to instantly terminate other open tabs.
   * 5. Notify currentUserSubject subscribers of unauthenticated state.
   */
  logout(specificRole?: string): void {
    // 1. Stop SignalR connection before token removal
    try {
      const notificationService = this.injector.get(NotificationService);
      if (notificationService) {
        notificationService.stopConnection();
      }
    } catch { }

    // 2. Clear sessionStorage
    try {
      sessionStorage.clear();
    } catch { }

    // 3. Targeted localStorage purge for HealSync-prefixed keys
    try {
      Object.keys(localStorage)
        .filter(key => key.startsWith('healsync_'))
        .forEach(key => localStorage.removeItem(key));
    } catch { }

    // 4. Explicit Cross-Tab Logout Broadcast
    try {
      localStorage.setItem('healsync_logout_event', Date.now().toString());
    } catch { }

    // 5. Emit null to currentUserSubject
    this.currentUserSubject.next(null);
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
    const activeUser = this.getAnyActiveUser();
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
    const activeUser = this.getAnyActiveUser();
    const decoded = this.getDecodedToken(specificRole);
    if (decoded) {
      return decoded['nameid'] || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/nameidentifier'] || decoded['sub'] || activeUser?.userId;
    }
    return activeUser?.userId || null;
  }

  getRole(specificRole?: string): string | null {
    const decoded = this.getDecodedToken(specificRole);
    if (decoded) {
      const jwtRole = decoded['role'] || decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      if (jwtRole) return jwtRole;
    }

    const activeUser = this.getAnyActiveUser();
    return activeUser?.role || null;
  }

  getToken(specificRole?: string): string | null {
    const activeUser = this.getAnyActiveUser();
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
    const activeUser = this.getAnyActiveUser();
    if (!activeUser || !activeUser.token) return false;
    if (specificRole && activeUser.role !== specificRole) return false;

    // Verify token expiration
    const decoded = this.getDecodedToken(specificRole);
    if (decoded && decoded.exp) {
      if (decoded.exp * 1000 <= Date.now()) {
        this.logout();
        return false;
      }
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

  sendAuthOtp(payload: { targetIdentifier: string; channel: string; purpose?: string }): Observable<any> {
    return this.http.post<any>('/api/auth/send-otp', payload);
  }

  verifyAuthOtp(payload: { targetIdentifier: string; otpCode: string; purpose?: string }): Observable<any> {
    return this.http.post<any>('/api/auth/verify-otp', payload);
  }

  loginWithWhatsApp(payload: { mobileNo: string; otpCode: string }): Observable<any> {
    return this.http.post<any>('/api/auth/login-whatsapp', payload).pipe(
      tap((res) => {
        if (res && res.token) {
          const userObj = { ...res, role: res.role || 'Patient' };
          const key = `healsync_auth_${userObj.role}`;
          sessionStorage.setItem(key, JSON.stringify(userObj));
          localStorage.setItem(key, JSON.stringify(userObj));
          this.currentUserSubject.next(userObj);
        }
      })
    );
  }

  initiateContactUpdate(payload: { newEmail?: string; newMobileNo?: string; channel?: string }): Observable<any> {
    return this.http.post<any>('/api/patients/initiate-contact-update', payload);
  }

  confirmContactUpdate(payload: { newEmail?: string; newMobileNo?: string; emailOtp?: string; mobileOtp?: string }): Observable<any> {
    return this.http.post<any>('/api/patients/confirm-contact-update', payload);
  }

  verifyTwoFactor(payload: { userId: string; otpCode: string }): Observable<any> {
    return this.http.post<any>('/api/auth/verify-2fa', payload).pipe(
      tap((res) => {
        if (res && res.token) {
          const userObj = { ...res, role: res.role || 'Patient' };
          const key = `healsync_auth_${userObj.role}`;
          sessionStorage.setItem(key, JSON.stringify(userObj));
          localStorage.setItem(key, JSON.stringify(userObj));
          this.currentUserSubject.next(userObj);
        }
      })
    );
  }

  getTwoFactorStatus(): Observable<any> {
    return this.http.get<any>('/api/users/2fa-status');
  }

  toggleTwoFactor(enabled: boolean): Observable<any> {
    return this.http.post<any>('/api/users/toggle-2fa', { enabled });
  }
}
