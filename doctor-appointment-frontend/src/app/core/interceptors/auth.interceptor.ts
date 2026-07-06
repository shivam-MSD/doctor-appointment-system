import { Injectable } from '@angular/core';
import { HttpEvent, HttpInterceptor, HttpHandler, HttpRequest } from '@angular/common/http';
import { Observable } from 'rxjs';
import { AuthService } from '../services/auth.service';

@Injectable()
export class AuthInterceptor implements HttpInterceptor {
  constructor(private authService: AuthService) {}

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

    return next.handle(authReq);
  }
}
