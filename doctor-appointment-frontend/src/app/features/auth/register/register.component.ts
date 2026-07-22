import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent implements OnInit, OnDestroy {
  email = '';
  password = '';
  confirmPassword = '';
  firstName = '';
  lastName = '';
  mobileNo = '';
  role = 'Patient'; // Default role
  errorMessage = '';
  successMessage = '';
  step: 'register' | 'otp' = 'register';
  otp = '';
  isSubmitting = false;

  resendCooldown = 0;
  cooldownInterval: any;

  constructor(
    private authService: AuthService,
    private router: Router
  ) {}

  ngOnDestroy(): void {
    if (this.cooldownInterval) {
      clearInterval(this.cooldownInterval);
    }
  }

  startCooldown(): void {
    this.resendCooldown = 30;
    if (this.cooldownInterval) {
      clearInterval(this.cooldownInterval);
    }
    this.cooldownInterval = setInterval(() => {
      if (this.resendCooldown > 0) {
        this.resendCooldown--;
      } else {
        clearInterval(this.cooldownInterval);
      }
    }, 1000);
  }

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
    }
  }

  get passwordsMatch(): boolean {
    return this.password === this.confirmPassword;
  }

  onSubmit(form: any): void {
    if (form.invalid) {
      Object.keys(form.controls).forEach(key => {
        form.controls[key].markAsTouched();
      });
      this.errorMessage = 'Please complete all required fields correctly.';
      return;
    }

    if (this.password !== this.confirmPassword) {
      this.errorMessage = 'Password and Confirm Password do not match.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    const payload = {
      email: this.email,
      password: this.password,
      confirmPassword: this.confirmPassword,
      firstName: this.firstName,
      lastName: this.lastName,
      mobileNo: this.mobileNo,
      role: this.role
    };

    this.authService.register(payload).subscribe({
      next: () => {
        this.errorMessage = '';
        this.successMessage = 'Account created successfully! Redirecting to login...';
        this.isSubmitting = false;
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 1500);
      },
      error: (err) => {
        this.isSubmitting = false;
        if (err?.status === 403 || err?.error?.detail?.includes('EmailVerificationRequired') || err?.error?.title?.includes('Email Verification Required')) {
          this.errorMessage = '';
          this.successMessage = 'A verification OTP has been sent to your email address.';
          this.step = 'otp';
          this.otp = '';
          this.startCooldown();
        } else {
          this.successMessage = '';
          this.errorMessage = err?.error?.detail || 'Registration failed. Please try again.';
        }
      }
    });
  }

  onVerifyOtp(): void {
    if (!this.otp || this.otp.length !== 6) {
      this.errorMessage = 'Please enter a valid 6-digit OTP code.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.authService.verifyEmail({ email: this.email, otp: this.otp }).subscribe({
      next: (res) => {
        this.errorMessage = '';
        this.successMessage = 'Email verified and registered successfully! Redirecting to login...';
        this.isSubmitting = false;
        // Log out immediately so the user is forced to log in manually
        this.authService.logout();
        setTimeout(() => {
          this.router.navigate(['/login'], { queryParams: { email: this.email, message: `${this.email} registered successfully.` } });
        }, 2000);
      },
      error: (err) => {
        this.isSubmitting = false;
        this.successMessage = '';
        this.errorMessage = err?.error?.detail || 'Invalid or expired OTP code.';
      }
    });
  }

  resendOtp(): void {
    if (this.resendCooldown > 0) {
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    const payload = {
      email: this.email,
      password: this.password,
      confirmPassword: this.confirmPassword,
      firstName: this.firstName,
      lastName: this.lastName,
      mobileNo: this.mobileNo,
      role: this.role
    };

    this.authService.register(payload).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.successMessage = 'A new verification OTP has been sent successfully!';
        this.startCooldown();
      },
      error: (err) => {
        this.isSubmitting = false;
        if (err?.status === 403 || err?.error?.detail?.includes('EmailVerificationRequired') || err?.error?.title?.includes('Email Verification Required')) {
          this.successMessage = 'A new verification OTP has been sent to your email.';
          this.startCooldown();
        } else {
          this.errorMessage = err?.error?.detail || 'Failed to resend verification OTP. Please try again.';
        }
      }
    });
  }
}
