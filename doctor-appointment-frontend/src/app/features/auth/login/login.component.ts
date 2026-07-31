import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  styleUrls: ['./login.component.css']
})
export class LoginComponent implements OnInit {
  email = '';
  password = '';
  errorMessage = '';
  successMessage = '';
  selectedRole: 'Patient' | 'Doctor' | 'Admin' | 'SuperAdmin' = 'Patient';
  isFixedRole = false;

  // Verification Dialog state
  showVerificationModal = false;
  verificationEmail = '';
  verificationOtp = '';
  verificationError = '';
  verificationSuccess = '';

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    // Read fixed role from route data if accessed via dedicated URL
    this.route.data.subscribe(data => {
      if (data && data['role']) {
        this.selectedRole = data['role'];
        this.isFixedRole = true;
      }
    });

    // Listen to query params for prefilled email, success, and error messages
    this.route.queryParams.subscribe(params => {
      if (params['role']) {
        this.selectedRole = params['role'];
      }
      if (params['message']) {
        this.successMessage = params['message'];
      }
      if (params['error']) {
        this.errorMessage = params['error'];
      }
      if (params['email']) {
        this.email = params['email'];
      }
    });

    // Only redirect if ALREADY authenticated for THIS specific login role
    if (this.authService.isAuthenticated(this.selectedRole)) {
      const role = this.selectedRole;
      if (role === 'Patient') {
        this.router.navigate(['/patient/dashboard']);
      } else if (role === 'Doctor') {
        this.router.navigate(['/doctor/dashboard']);
      } else if (role === 'Admin') {
        this.router.navigate(['/admin/dashboard']);
      } else if (role === 'SuperAdmin') {
        this.router.navigate(['/superadmin/dashboard']);
      }
      return;
    }

    // Real-time cross-tab login auto-redirect (Scenario 2B)
    window.addEventListener('storage', (event: StorageEvent) => {
      if (event.key === `healsync_auth_${this.selectedRole}` && event.newValue) {
        if (this.authService.isAuthenticated(this.selectedRole)) {
          const role = this.selectedRole;
          if (role === 'Patient') {
            this.router.navigate(['/patient/dashboard']);
          } else if (role === 'Doctor') {
            this.router.navigate(['/doctor/dashboard']);
          } else if (role === 'Admin') {
            this.router.navigate(['/admin/dashboard']);
          } else if (role === 'SuperAdmin') {
            this.router.navigate(['/superadmin/dashboard']);
          }
        }
      }
    });

    // Listen to query params for prefilled email, success, and error messages
    this.route.queryParams.subscribe(params => {
      let shouldCleanUrl = false;
      if (params['message']) {
        this.successMessage = params['message'];
        shouldCleanUrl = true;
      }
      if (params['error']) {
        this.errorMessage = params['error'];
        shouldCleanUrl = true;
      }
      if (params['email']) {
        this.email = params['email'];
      }
      if (params['role']) {
        this.selectedRole = params['role'] as any;
      }

      if (shouldCleanUrl) {
        // Clean error/message query parameters from browser address bar so page refresh (F5) will not display them again
        this.router.navigate([], {
          relativeTo: this.route,
          queryParams: { error: null, message: null },
          queryParamsHandling: 'merge',
          replaceUrl: true
        });
      }
    });
  }

  getPortalTitle(): string {
    switch (this.selectedRole) {
      case 'Patient': return 'Patient Portal';
      case 'Doctor': return 'Doctor Portal';
      case 'Admin': return 'Clinic Admin Portal';
      case 'SuperAdmin': return 'Super Admin Console';
      default: return 'Welcome Back';
    }
  }

  getRegisterLink(): string | null {
    if (this.selectedRole === 'Patient') return '/patient/register';
    if (this.selectedRole === 'Doctor') return '/doctor/register';
    return null; // Admin/SuperAdmin cannot self-register
  }

  isLoading = false;

  selectRole(role: 'Patient' | 'Doctor' | 'Admin' | 'SuperAdmin'): void {
    this.selectedRole = role;
    this.errorMessage = '';
  }

  onSubmit(form: any): void {
    if (this.isLoading) return;
    this.errorMessage = '';
    this.successMessage = '';

    if (form.invalid) {
      Object.keys(form.controls).forEach(key => {
        form.controls[key].markAsTouched();
      });
      this.errorMessage = 'Please enter a valid email and password.';
      return;
    }

    this.isLoading = true;
    this.authService.login({ email: this.email, password: this.password, role: this.selectedRole }).subscribe({
      next: (user) => {
        this.isLoading = false;
        if (user.role !== this.selectedRole) {
          this.authService.logout();
          this.errorMessage = `Unauthorized access. Invalid credentials for the ${this.getPortalTitle()}.`;
          return;
        }
        this.toastService.showSuccess('Logged in successfully!');
        if (user.role === 'Patient') {
          this.router.navigate(['/patient/dashboard']);
        } else if (user.role === 'Doctor') {
          this.router.navigate(['/doctor/dashboard']);
        } else if (user.role === 'Admin') {
          this.router.navigate(['/admin/dashboard']);
        } else if (user.role === 'SuperAdmin') {
          this.router.navigate(['/superadmin/dashboard']);
        }
      },
      error: (err) => {
        this.isLoading = false;
        if (err.status === 403 && err.error?.requiresVerification) {
          this.verificationEmail = err.error.email || this.email;
          this.showVerificationModal = true;
          this.toastService.showError(err, 'Email verification required.');
        } else {
          this.errorMessage = this.toastService.extractErrorMessage(err, 'Invalid credentials or connection error.');
          this.toastService.showError(this.errorMessage);
        }
      }
    });
  }

  onVerifySubmit(): void {
    if (!this.verificationOtp || this.verificationOtp.length !== 6) {
      this.verificationError = 'Please enter a valid 6-digit OTP code.';
      return;
    }

    this.verificationError = '';
    this.verificationSuccess = '';

    this.authService.verifyEmail({ email: this.verificationEmail, otp: this.verificationOtp }).subscribe({
      next: (user) => {
        this.verificationSuccess = 'Email verified successfully! Logging in...';
        this.toastService.showSuccess(this.verificationSuccess);
        setTimeout(() => {
          this.showVerificationModal = false;
          if (user.role === 'Patient') {
            this.router.navigate(['/patient/dashboard']);
          } else if (user.role === 'Doctor') {
            this.router.navigate(['/doctor/dashboard']);
          } else if (user.role === 'Admin') {
            this.router.navigate(['/admin/dashboard']);
          } else if (user.role === 'SuperAdmin') {
            this.router.navigate(['/superadmin/dashboard']);
          } else {
            this.router.navigate(['/dashboard']);
          }
        }, 1500);
      },
      error: (err) => {
        this.verificationError = err?.error?.detail || 'Invalid or expired OTP code.';
      }
    });
  }

  closeVerificationModal(): void {
    this.showVerificationModal = false;
    this.verificationError = '';
    this.verificationSuccess = '';
  }
}
