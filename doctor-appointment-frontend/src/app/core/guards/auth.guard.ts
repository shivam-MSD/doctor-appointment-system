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
    if (!this.authService.isAuthenticated()) {
      this.redirectToLogin(state.url);
      return false;
    }

    const role = this.authService.getRole();
    const url = state.url.toLowerCase();

    // Check Role-Based Access Control
    if (url.includes('/patient/') && role !== 'Patient') {
      this.redirectBasedOnRole(role);
      return false;
    }
    if (url.includes('/doctor/') && role !== 'Doctor') {
      this.redirectBasedOnRole(role);
      return false;
    }
    if (url.includes('/admin/') && role !== 'Admin') {
      this.redirectBasedOnRole(role);
      return false;
    }
    if (url.includes('/superadmin/') && role !== 'SuperAdmin') {
      this.redirectBasedOnRole(role);
      return false;
    }

    return true;
  }

  private redirectToLogin(currentUrl: string) {
    if (currentUrl.includes('/patient/')) {
      this.router.navigate(['/patient/login']);
    } else if (currentUrl.includes('/doctor/')) {
      this.router.navigate(['/doctor/login']);
    } else if (currentUrl.includes('/admin/')) {
      this.router.navigate(['/admin/login']);
    } else if (currentUrl.includes('/superadmin/')) {
      this.router.navigate(['/superadmin/login']);
    } else {
      this.router.navigate(['/login']);
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
