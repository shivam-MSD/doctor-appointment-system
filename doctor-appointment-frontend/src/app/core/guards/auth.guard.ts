import { Injectable } from '@angular/core';
import { CanActivate, ActivatedRouteSnapshot, RouterStateSnapshot, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';

@Injectable({
  providedIn: 'root'
})
export class AuthGuard implements CanActivate {
  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  canActivate(route: ActivatedRouteSnapshot, state: RouterStateSnapshot): boolean {
    const expectedRole = route.data['expectedRole'];
    const targetLoginRoute = route.data['loginRoute'] || this.getLoginRouteForRole(expectedRole);

    if (!this.authService.isAuthenticated(expectedRole)) {
      this.router.navigate([targetLoginRoute], { replaceUrl: true });
      return false;
    }

    const activeRole = this.authService.getRole(expectedRole);

    if (expectedRole && activeRole !== expectedRole) {
      this.router.navigate([targetLoginRoute], { replaceUrl: true });
      return false;
    }

    return true;
  }

  private getLoginRouteForRole(role?: string): string {
    if (role === 'Admin') return '/admin/login';
    if (role === 'SuperAdmin') return '/superadmin/login';
    return '/login'; // Patient & Doctor redirect to main shared /login page
  }
}
