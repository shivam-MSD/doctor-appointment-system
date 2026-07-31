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
    const loginRoute = route.data['loginRoute'];

    if (!this.authService.isAuthenticated(expectedRole)) {
      this.redirectToLogin(state.url, loginRoute);
      return false;
    }

    const role = this.authService.getRole(expectedRole);

    // If route specifies an expected role, enforce it cleanly
    if (expectedRole && role !== expectedRole) {
      this.redirectBasedOnRole(role);
      return false;
    }

    return true;
  }

  private redirectToLogin(currentUrl: string, targetLoginRoute?: string) {
    if (targetLoginRoute) {
      this.router.navigate([targetLoginRoute]);
      return;
    }
    const role = this.authService.getRole();
    if (role) {
      this.router.navigate([`/${role.toLowerCase()}/login`]);
    } else {
      this.router.navigate(['/patient/login']);
    }
  }

  private redirectBasedOnRole(role: string | null) {
    switch (role) {
      case 'Patient':
        this.router.navigate(['/patient/dashboard']);
        break;
      case 'Doctor':
        this.router.navigate(['/doctor/dashboard']);
        break;
      case 'Admin':
        this.router.navigate(['/admin/dashboard']);
        break;
      case 'SuperAdmin':
        this.router.navigate(['/superadmin/dashboard']);
        break;
      default:
        this.router.navigate(['/login']);
    }
  }
}
