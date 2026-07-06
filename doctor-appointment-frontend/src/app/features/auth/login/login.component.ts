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

    // Comment out queryParams listener for OTP verification redirection
    // this.route.queryParams.subscribe(params => {
    //   if (params['verify'] === 'true' && params['email']) {
    //     this.verificationEmail = params['email'];
    //     this.showVerificationModal = true;
    //   }
    // });
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
    if (form.invalid) {
      Object.keys(form.controls).forEach(key => {
        form.controls[key].markAsTouched();
      });
      this.errorMessage = 'Please enter a valid email and password.';
      this.toastService.showError(this.errorMessage);
      return;
    }

    this.authService.login({ email: this.email, password: this.password, role: this.selectedRole }).subscribe({
      next: (user) => {
        if (user.role !== this.selectedRole) {
          this.authService.logout();
          this.errorMessage = `Unauthorized access. Invalid credentials for the ${this.getPortalTitle()}.`;
          this.toastService.showError(this.errorMessage);
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
        this.toastService.showError(this.errorMessage);
      }
    });
  }

  onVerifySubmit(): void {
    if (!this.verificationOtp || this.verificationOtp.length !== 6) {
      this.verificationError = 'Please enter a valid 6-digit OTP code.';
      this.toastService.showError(this.verificationError);
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
        this.toastService.showError(this.verificationError);
      }
    });
  }

  closeVerificationModal(): void {
    this.showVerificationModal = false;
    this.verificationError = '';
    this.verificationSuccess = '';
  }
}
