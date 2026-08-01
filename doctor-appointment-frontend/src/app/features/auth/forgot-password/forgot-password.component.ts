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
    // If user is ALREADY logged in, redirect them to their profile/security settings page instead of showing unauthenticated forgot password form
    const activeUser = this.authService.getAnyActiveUser();
    if (activeUser && activeUser.role) {
      this.toastService.showSuccess('You are already logged in. Redirecting to Profile Settings...');
      const profilePath = `/${activeUser.role.toLowerCase()}/profile`;
      this.router.navigate([profilePath], { replaceUrl: true });
      return;
    }

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

  get passwordStrength(): 'weak' | 'medium' | 'strong' {
    if (!this.newPassword) return 'weak';
    let score = 0;
    if (this.newPassword.length >= 8) score++;
    if (/[A-Z]/.test(this.newPassword)) score++;
    if (/[0-9]/.test(this.newPassword)) score++;
    if (/[^A-Za-z0-9]/.test(this.newPassword)) score++;
    if (score >= 3) return 'strong';
    if (score >= 2) return 'medium';
    return 'weak';
  }

  get passwordsMatch(): boolean {
    return this.newPassword === this.confirmPassword;
  }

  resendOtp(): void {
    if (this.resendCooldown > 0 || this.isLoading) return;
    this.onSendOtp();
  }

  goToLogin(): void {
    let target = '/login';
    if (this.role === 'Admin') target = '/admin/login';
    if (this.role === 'SuperAdmin') target = '/superadmin/login';
    this.router.navigate([target]);
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
    this.authService.checkEmail(this.email, this.role).subscribe({
      next: (res) => {
        this.step = 'password';
        this.isLoading = false;
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || `No account found with this email address under the '${this.role}' role.`);
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
    this.authService.forgotPassword(this.email, this.role).subscribe({
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
    this.authService.resetPassword(this.email, this.otp, this.newPassword, this.role).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message || 'Password reset successfully!');
        this.step = 'success';
        this.isLoading = false;
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Invalid or expired OTP code.');
        this.isLoading = false;
      }
    });
  }
}
