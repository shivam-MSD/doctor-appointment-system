import { Injectable } from '@angular/core';
import { HttpEvent, HttpInterceptor, HttpHandler, HttpRequest, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  intercept(req: HttpRequest<any>, next: HttpHandler): Observable<HttpEvent<any>> {
    const token = this.authService.getToken();
    const userId = this.authService.getUserId();

    let authReq = req;

    // 1. Add JWT token to Authorization header if logged in
    if (token) {
      authReq = authReq.clone({
        headers: authReq.headers.set('Authorization', `Bearer ${token}`)
      });
    }

    // 2. Add X-User-Id header to simulate active session identity for mock auth
    if (userId) {
      authReq = authReq.clone({
        headers: authReq.headers.set('X-User-Id', userId)
      });
    }

    return next.handle(authReq).pipe(
      catchError((error: any) => {
        if (error instanceof HttpErrorResponse && error.status === 401) {
          const role = this.authService.getRole();
          this.authService.logout();
          // Do not redirect to the generic /login page if the error came from an authentication endpoint
          if (!req.url.toLowerCase().includes('/api/auth/')) {
            const queryParams = { error: 'Your session has expired. Please log in again.' };
            if (role === 'Doctor') {
              this.router.navigate(['/doctor/login'], { queryParams });
            } else if (role === 'Admin') {
              this.router.navigate(['/admin/login'], { queryParams });
            } else if (role === 'SuperAdmin') {
              this.router.navigate(['/superadmin/login'], { queryParams });
            } else {
              this.router.navigate(['/patient/login'], { queryParams });
            }
          }
        }
        return throwError(() => error);
      })
    );
  }
}
