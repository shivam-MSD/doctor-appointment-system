import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { AppointmentService } from '../../../core/services/appointment.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-doctor-register',
  templateUrl: './doctor-register.component.html',
  styleUrls: ['./doctor-register.component.css']
})
export class DoctorRegisterComponent implements OnInit {
  email = '';
  password = '';
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

  constructor(
    private authService: AuthService,
    private router: Router,
    private appointmentService: AppointmentService,
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
      this.toastService.showError(this.errorMessage);
      return;
    }

    const payload = {
      email: this.email,
      password: this.password,
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
        this.successMessage = 'Doctor application submitted successfully! Redirecting to login...';
        this.toastService.showSuccess(this.successMessage);
        setTimeout(() => {
          this.router.navigate(['/doctor/login']);
        }, 2000);
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail || 'An error occurred during onboarding. Please try again.';
        this.toastService.showError(this.errorMessage);
      }
    });
  }
}
