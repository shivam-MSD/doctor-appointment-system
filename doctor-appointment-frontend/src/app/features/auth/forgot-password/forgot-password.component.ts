import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
  styleUrls: ['./forgot-password.component.css']
})
export class ForgotPasswordComponent implements OnInit, OnDestroy {
  step: 'email' | 'password' | 'otp' | 'success' = 'email';
  email = '';
  otp = '';
  newPassword = '';
  confirmPassword = '';
  isLoading = false;
  showPassword = false;
  showConfirmPassword = false;
  role: 'Patient' | 'Doctor' | 'Admin' | 'SuperAdmin' = 'Patient';

  resendCooldown = 0;
  cooldownInterval: any;

  constructor(
    private authService: AuthService,
    private router: Router,
    private route: ActivatedRoute,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      if (params['role']) {
        this.role = params['role'] as any;
      }
    });
  }

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

  onValidateEmail(): void {
    if (!this.email) {
      this.toastService.showError('Please enter your registered email address.');
      return;
    }
    this.isLoading = true;
    this.authService.checkEmail(this.email).subscribe({
      next: (res) => {
        if (res && res.role) {
          this.role = res.role;
        }
        this.step = 'password';
        this.isLoading = false;
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'No account found with this email address.');
        this.isLoading = false;
      }
    });
  }

  onSendOtp(): void {
    if (!this.newPassword || this.newPassword.length < 6) {
      this.toastService.showError('Password must be at least 6 characters long.');
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.toastService.showError('Passwords do not match.');
      return;
    }
    this.isLoading = true;
    this.authService.forgotPassword(this.email).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message || 'OTP sent to your email!');
        this.step = 'otp';
        this.isLoading = false;
        this.startCooldown();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to send OTP. Please check your email and try again.');
        this.isLoading = false;
      }
    });
  }

  onResetPassword(): void {
    if (!this.otp || this.otp.length !== 6) {
      this.toastService.showError('Please enter a valid 6-digit OTP code.');
      return;
    }
    this.isLoading = true;
    this.authService.resetPassword(this.email, this.otp, this.newPassword).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message || 'Password reset successfully!');
        this.step = 'success';
        this.isLoading = false;
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to reset password. Please check your OTP and try again.');
        this.isLoading = false;
      }
    });
  }

  resendOtp(): void {
    if (this.resendCooldown > 0 || this.isLoading) return;
    this.isLoading = true;
    this.authService.forgotPassword(this.email).subscribe({
      next: (res) => {
        this.toastService.showSuccess('A new OTP has been sent successfully!');
        this.isLoading = false;
        this.startCooldown();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to resend OTP. Please try again.');
        this.isLoading = false;
      }
    });
  }

  goToLogin(): void {
    if (this.role === 'Doctor') {
      this.router.navigate(['/doctor/login']);
    } else if (this.role === 'Admin') {
      this.router.navigate(['/admin/login']);
    } else if (this.role === 'SuperAdmin') {
      this.router.navigate(['/superadmin/login']);
    } else if (this.role === 'Patient') {
      this.router.navigate(['/patient/login']);
    } else {
      this.router.navigate(['/login']);
    }
  }

  get passwordsMatch(): boolean {
    return this.newPassword === this.confirmPassword;
  }

  get passwordStrength(): string {
    if (!this.newPassword) return '';
    if (this.newPassword.length < 6) return 'weak';
    if (this.newPassword.length >= 10 && /[A-Z]/.test(this.newPassword) && /[0-9]/.test(this.newPassword)) return 'strong';
    return 'medium';
  }
}
