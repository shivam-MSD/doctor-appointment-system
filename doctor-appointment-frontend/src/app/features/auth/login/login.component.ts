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

  // Login Mode Toggle
  loginMode: 'password' | 'whatsapp' = 'password';
  whatsAppMobileNo = '';
  whatsAppOtpCode = '';
  whatsAppOtpSent = false;
  isSendingWhatsAppOtp = false;

  // 2FA Dialog state
  showTwoFactorModal = false;
  twoFactorUserId = '';
  twoFactorChannels: string[] = [];
  twoFactorOtp = '';
  twoFactorError = '';

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
      let shouldCleanUrl = false;
      if (params['role']) {
        this.selectedRole = params['role'] as any;
      }
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

      if (shouldCleanUrl) {
        // Clean error/message query parameters from browser address bar
        this.router.navigate([], {
          relativeTo: this.route,
          queryParams: { error: null, message: null },
          queryParamsHandling: 'merge',
          replaceUrl: true
        });
      }
    });

    // Auto-redirect if ANY active user session is already logged in across the browser!
    const activeUser = this.authService.getAnyActiveUser();
    if (activeUser && activeUser.role) {
      this.redirectToRoleDashboard(activeUser.role);
      return;
    }

    // Real-time cross-tab login auto-redirect: when Tab 1 logs in, Tab 2 auto-logs in without refreshing!
    window.addEventListener('storage', (event: StorageEvent) => {
      if (event.key && event.key.startsWith('healsync_auth_') && event.newValue) {
        try {
          const user = JSON.parse(event.newValue);
          if (user && user.role) {
            sessionStorage.setItem(event.key, event.newValue);
            this.toastService.showSuccess(`Logged in as ${user.role} from another tab!`);
            this.redirectToRoleDashboard(user.role);
          }
        } catch { }
      }
    });
  }

  private redirectToRoleDashboard(role: string): void {
    if (role === 'Patient') {
      this.router.navigate(['/patient/dashboard'], { replaceUrl: true });
    } else if (role === 'Doctor') {
      this.router.navigate(['/doctor/dashboard'], { replaceUrl: true });
    } else if (role === 'Admin') {
      this.router.navigate(['/admin/dashboard'], { replaceUrl: true });
    } else if (role === 'SuperAdmin') {
      this.router.navigate(['/superadmin/dashboard'], { replaceUrl: true });
    }
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

    // If an active session already exists, prevent re-submitting and redirect immediately
    const activeUser = this.authService.getAnyActiveUser();
    if (activeUser && activeUser.role) {
      this.toastService.showSuccess(`Already logged in as ${activeUser.role}. Redirecting to dashboard...`);
      this.redirectToRoleDashboard(activeUser.role);
      return;
    }

    if (form.invalid) {
      Object.keys(form.controls).forEach(key => {
        form.controls[key].markAsTouched();
      });
      this.errorMessage = 'Please enter a valid email and password.';
      return;
    }

    this.isLoading = true;
    this.authService.login({ email: this.email, password: this.password, role: this.selectedRole }).subscribe({
      next: (res) => {
        this.isLoading = false;
        if (res.requiresTwoFactor) {
          this.twoFactorUserId = res.userId;
          this.twoFactorChannels = res.twoFactorChannels || ['Email'];
          this.showTwoFactorModal = true;
          this.toastService.showSuccess(`2FA Code dispatched to ${this.twoFactorChannels.join(' & ')}!`);
          return;
        }
        if (res.role !== this.selectedRole) {
          this.authService.logout();
          this.errorMessage = `Unauthorized access. Invalid credentials for the ${this.getPortalTitle()}.`;
          return;
        }
        this.toastService.showSuccess('Logged in successfully!');
        this.redirectToRoleDashboard(res.role);
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
          this.redirectToRoleDashboard(user.role);
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

  onSendWhatsAppOtp(): void {
    if (!this.whatsAppMobileNo || this.whatsAppMobileNo.length < 10) {
      this.toastService.showError('Please enter a valid WhatsApp mobile number (min 10 digits).');
      return;
    }

    this.isSendingWhatsAppOtp = true;
    this.errorMessage = '';

    this.authService.sendAuthOtp({
      targetIdentifier: this.whatsAppMobileNo,
      channel: 'WhatsApp',
      purpose: 'Login'
    }).subscribe({
      next: (res) => {
        this.isSendingWhatsAppOtp = false;
        this.whatsAppOtpSent = true;
        this.toastService.showSuccess(res.message || 'OTP dispatched to your WhatsApp number!');
      },
      error: (err) => {
        this.isSendingWhatsAppOtp = false;
        this.errorMessage = this.toastService.extractErrorMessage(err, 'Failed to send WhatsApp OTP.');
        this.toastService.showError(this.errorMessage);
      }
    });
  }

  onWhatsAppLogin(): void {
    if (!this.whatsAppMobileNo || !this.whatsAppOtpCode) {
      this.toastService.showError('Please enter your WhatsApp mobile number and 6-digit OTP code.');
      return;
    }

    this.isLoading = true;
    this.errorMessage = '';

    this.authService.loginWithWhatsApp({
      mobileNo: this.whatsAppMobileNo,
      otpCode: this.whatsAppOtpCode
    }).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.toastService.showSuccess('Logged in successfully via WhatsApp!');
        this.redirectToRoleDashboard(res.role || 'Patient');
      },
      error: (err) => {
        this.isLoading = false;
        this.errorMessage = this.toastService.extractErrorMessage(err, 'WhatsApp Login failed.');
        this.toastService.showError(this.errorMessage);
      }
    });
  }

  onVerifyTwoFactor(): void {
    if (!this.twoFactorOtp || this.twoFactorOtp.length !== 6) {
      this.twoFactorError = 'Please enter a valid 6-digit 2FA code.';
      return;
    }

    this.isLoading = true;
    this.twoFactorError = '';

    this.authService.verifyTwoFactor({
      userId: this.twoFactorUserId,
      otpCode: this.twoFactorOtp
    }).subscribe({
      next: (res) => {
        this.isLoading = false;
        this.showTwoFactorModal = false;
        this.toastService.showSuccess('2FA Verification successful! Logged in.');
        this.redirectToRoleDashboard(res.role || 'Patient');
      },
      error: (err) => {
        this.isLoading = false;
        this.twoFactorError = err?.error?.detail || 'Invalid 2FA security code. Please try again.';
      }
    });
  }
}
