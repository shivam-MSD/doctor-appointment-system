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
    if (this.authService.isAuthenticated()) {
      const role = this.authService.getRole();
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

    // Read fixed role from route data if accessed via dedicated URL
    this.route.data.subscribe(data => {
      if (data && data['role']) {
        this.selectedRole = data['role'];
        this.isFixedRole = true;
      }
    });

    // Listen to query params for prefilled email and registration success messages
    this.route.queryParams.subscribe(params => {
      if (params['message']) {
        this.successMessage = params['message'];
      }
      if (params['email']) {
        this.email = params['email'];
      }
      if (params['role']) {
        this.selectedRole = params['role'] as any;
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

  selectRole(role: 'Patient' | 'Doctor' | 'Admin' | 'SuperAdmin'): void {
    this.selectedRole = role;
    this.errorMessage = '';
  }

  onSubmit(form: any): void {
    this.errorMessage = '';
    this.successMessage = '';

    if (form.invalid) {
      Object.keys(form.controls).forEach(key => {
        form.controls[key].markAsTouched();
      });
      this.errorMessage = 'Please enter a valid email and password.';
      return;
    }

    this.authService.login({ email: this.email, password: this.password, role: this.selectedRole }).subscribe({
      next: (user) => {
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
        } else {
          this.router.navigate(['/dashboard']);
        }
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail || 'Invalid email or password.';
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
