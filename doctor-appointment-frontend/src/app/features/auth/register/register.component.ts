import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-register',
  templateUrl: './register.component.html',
  styleUrls: ['./register.component.css']
})
export class RegisterComponent implements OnInit {
  email = '';
  password = '';
  firstName = '';
  lastName = '';
  mobileNo = '';
  role = 'Patient'; // Default role
  errorMessage = '';
  successMessage = '';

  constructor(
    private authService: AuthService,
    private router: Router
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
    }
  }

  onSubmit(form: any): void {
    if (form.invalid) {
      Object.keys(form.controls).forEach(key => {
        form.controls[key].markAsTouched();
      });
      this.errorMessage = 'Please complete all required fields correctly.';
      return;
    }

    const payload = {
      email: this.email,
      password: this.password,
      firstName: this.firstName,
      lastName: this.lastName,
      mobileNo: this.mobileNo,
      role: this.role
    };

    this.authService.register(payload).subscribe({
      next: () => {
        this.errorMessage = '';
        this.successMessage = 'Account created successfully! Redirecting to login...';
        setTimeout(() => {
          this.router.navigate(['/login']);
        }, 1500);
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail || 'An error occurred during registration. Please try again.';
      }
    });
  }
}
