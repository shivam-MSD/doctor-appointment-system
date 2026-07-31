import { Injectable } from '@angular/core';
import { HttpEvent, HttpInterceptor, HttpHandler, HttpRequest, HttpErrorResponse } from '@angular/common/http';
import { Observable, throwError } from 'rxjs';
import { catchError } from 'rxjs/operators';
import { AuthService } from '../services/auth.service';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';

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

    // Prepend the environment-configured API base URL (e.g. Render server in prod, relative path in dev)
    if (req.url.startsWith('/api/') && environment.apiUrl) {
      authReq = authReq.clone({
        url: `${environment.apiUrl}${req.url}`
      });
    }

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
          // 1. Traverse Angular route snapshot tree to extract expectedRole metadata
          let expectedRole: string | undefined;
          let routeSnap = this.router.routerState.snapshot.root;
          while (routeSnap.firstChild) {
            routeSnap = routeSnap.firstChild;
            if (routeSnap.data && routeSnap.data['expectedRole']) {
              expectedRole = routeSnap.data['expectedRole'];
            }
          }

          const jwtRole = this.authService.getRole();
          const expiredRole = expectedRole || jwtRole || 'Patient';

          this.authService.logout(jwtRole || undefined);

          // Do not redirect if the error came from an authentication endpoint
          if (!req.url.toLowerCase().includes('/api/auth/')) {
            const queryParams = { error: 'Your session has expired. Please log in again.' };
            if (expiredRole === 'Admin') {
              this.router.navigate(['/admin/login'], { queryParams });
            } else if (expiredRole === 'SuperAdmin') {
              this.router.navigate(['/superadmin/login'], { queryParams });
            } else {
              this.router.navigate(['/login'], { queryParams: { ...queryParams, role: expiredRole } });
            }
          }
        }
        return throwError(() => error);
      })
    );
  }
}
