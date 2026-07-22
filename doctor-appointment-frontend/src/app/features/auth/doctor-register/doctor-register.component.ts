import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { AppointmentService } from '../../../core/services/appointment.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-doctor-register',
  templateUrl: './doctor-register.component.html',
  styleUrls: ['./doctor-register.component.css']
})
export class DoctorRegisterComponent implements OnInit, OnDestroy {
  email = '';
  firstName = '';
  lastName = '';
  mobileNo = '';
  gender = 'Male';
  dob = '';
  qualification = '';
  licenceNumber = '';
  yearsOfExperience = 1;
  consultationFee = 500.0; // Seed with default INR fee
  specializations: any[] = [];
  specializationId = '';
  
  errorMessage = '';
  successMessage = '';
  step: 'register' | 'otp' = 'register';
  otp = '';
  isSubmitting = false;

  resendCooldown = 0;
  cooldownInterval: any;

  constructor(
    private authService: AuthService,
    private router: Router,
    private appointmentService: AppointmentService,
    private toastService: ToastService
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
      return;
    }

    this.appointmentService.getSpecializations().subscribe({
      next: (data) => {
        this.specializations = data;
        if (this.specializations.length > 0) {
          this.specializationId = this.specializations[0].specializationId;
        }
      }
    });
  }

  onSubmit(form: any): void {
    if (form.invalid) {
      Object.keys(form.controls).forEach(key => {
        form.controls[key].markAsTouched();
      });
      this.errorMessage = 'Please complete all required fields correctly.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    const payload = {
      email: this.email,
      firstName: this.firstName,
      lastName: this.lastName,
      mobileNo: this.mobileNo,
      gender: this.gender,
      dob: this.dob,
      qualification: this.qualification,
      licenceNumber: this.licenceNumber,
      yearsOfExperience: this.yearsOfExperience,
      consultationFee: this.consultationFee,
      specializationId: this.specializationId
    };

    this.authService.registerDoctor(payload).subscribe({
      next: () => {
        this.errorMessage = '';
        this.successMessage = 'Registration successful! Your profile has been submitted for Super Admin verification. Once approved, your temporary login password will be securely emailed to you.';
        this.toastService.showSuccess('Registration successful! Check your email upon approval.');
        this.isSubmitting = false;
        setTimeout(() => {
          this.router.navigate(['/doctor/login']);
        }, 5000);
      },
      error: (err) => {
        this.isSubmitting = false;
        if (err?.status === 403 || err?.error?.detail?.includes('EmailVerificationRequired') || err?.error?.title?.includes('Email Verification Required')) {
          this.errorMessage = '';
          this.successMessage = 'A verification OTP has been sent to your email. Please verify your email below.';
          this.step = 'otp';
          this.otp = '';
          this.startCooldown();
        } else {
          this.successMessage = '';
          this.errorMessage = err?.error?.detail || 'An error occurred during onboarding. Please try again.';
        }
      }
    });
  }

  onVerifyOtp(): void {
    if (!this.otp || this.otp.length !== 6) {
      this.successMessage = '';
      this.errorMessage = 'Please enter a valid 6-digit OTP code.';
      return;
    }

    this.isSubmitting = true;
    this.errorMessage = '';
    this.successMessage = '';

    this.authService.verifyEmail({ email: this.email, otp: this.otp }).subscribe({
      next: () => {
        this.errorMessage = '';
        this.successMessage = 'Email verified and registration successful! Your application is pending Super Admin approval. Your credentials will be emailed once approved.';
        this.toastService.showSuccess('Email verified successfully!');
        this.isSubmitting = false;
        // Log out immediately so the session is cleared and the doctor is forced to log in upon admin approval
        this.authService.logout();
        setTimeout(() => {
          this.router.navigate(['/doctor/login']);
        }, 6000);
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
      firstName: this.firstName,
      lastName: this.lastName,
      mobileNo: this.mobileNo,
      gender: this.gender,
      dob: this.dob,
      qualification: this.qualification,
      licenceNumber: this.licenceNumber,
      yearsOfExperience: this.yearsOfExperience,
      consultationFee: this.consultationFee,
      specializationId: this.specializationId
    };

    this.authService.registerDoctor(payload).subscribe({
      next: () => {
        this.isSubmitting = false;
        this.successMessage = 'A new verification OTP has been sent successfully!';
        this.toastService.showSuccess('A new verification OTP has been sent successfully!');
        this.startCooldown();
      },
      error: (err) => {
        this.isSubmitting = false;
        if (err?.status === 403 || err?.error?.detail?.includes('EmailVerificationRequired') || err?.error?.title?.includes('Email Verification Required')) {
          this.successMessage = 'A new verification OTP has been sent to your email.';
          this.startCooldown();
        } else {
          this.errorMessage = err?.error?.detail || 'Failed to resend verification OTP. Please try again.';
          this.toastService.showError(this.errorMessage);
        }
      }
    });
  }
}
