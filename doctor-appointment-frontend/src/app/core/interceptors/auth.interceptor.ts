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
          this.authService.logout();
          this.router.navigate(['/login']);
        }
        return throwError(() => error);
      })
    );
  }
}
