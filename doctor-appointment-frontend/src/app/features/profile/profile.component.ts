import { Component, OnInit } from '@angular/core';
import { PatientService } from '../../core/services/patient.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { AppointmentService } from '../../core/services/appointment.service';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit {
  profileId = '';
  role = '';
  
  // Demographics Fields
  firstName = '';
  lastName = '';
  mobileNo = '';
  gender = '';
  dob = '';
  bloodGroup = '';
  emergencyContactName = '';
  emergencyContactNumber = '';

  // Address Fields
  country = 'India';
  state = '';
  city = '';
  area = '';
  pincode = '';
  addressline1 = '';
  addressline2 = '';

  // Doctor Fields
  qualification = '';
  licenceNumber = '';
  yearsOfExperience = 0;
  consultationFee = 0;
  about = '';
  specializations: any[] = [];
  specializationId = '';

  // Admin Fields
  clinicName = '';

  // Backend validation errors mapping
  backendErrors: { [key: string]: string } = {};

  // Decoupled Saved Stats State
  completionStats = { percentage: 30, left: [] as string[] };

  errorMessage = '';
  successMessage = '';

  constructor(
    private patientService: PatientService,
    private authService: AuthService,
    private toastService: ToastService,
    private appointmentService: AppointmentService
  ) {}

  ngOnInit(): void {
    this.profileId = sessionStorage.getItem('profileId') || '';
    this.role = this.authService.getRole() || '';
    this.loadProfile();

    if (this.role === 'Doctor') {
      this.loadSpecializations();
    }
  }

  loadSpecializations(): void {
    this.appointmentService.getSpecializations().subscribe({
      next: (data) => {
        this.specializations = data;
      }
    });
  }

  loadProfile(form?: any): void {
    if (this.role === 'Patient' && this.profileId) {
      this.patientService.getPatientProfile(this.profileId).subscribe({
        next: (data: any) => {
          this.firstName = data.firstName;
          this.lastName = data.lastName;
          this.mobileNo = data.mobileNo;
          this.gender = data.gender || 'Male';
          this.dob = data.dob ? data.dob.split('T')[0] : '';
          this.bloodGroup = data.bloodGroup || 'OPositive';
          this.emergencyContactName = data.emergencyContactName || '';
          this.emergencyContactNumber = data.emergencyContactNumber || '';

          // Address
          this.country = data.country || 'India';
          this.state = data.state || '';
          this.city = data.city || '';
          this.area = data.area || '';
          this.pincode = data.pincode || '';
          this.addressline1 = data.addressline1 || '';
          this.addressline2 = data.addressline2 || '';

          // Compute stats from loaded state
          this.completionStats = this.calculateStats(data);
          sessionStorage.setItem('profileCompletion', this.completionStats.percentage.toString());

          if (form && form.control) {
            form.control.markAsPristine();
            form.control.markAsUntouched();
          }
        },
        error: () => {
          this.toastService.showError('Failed to load profile details.');
        }
      });
    } else if (this.role === 'Doctor') {
      this.patientService.getDoctorProfile().subscribe({
        next: (data: any) => {
          this.firstName = data.firstName;
          this.lastName = data.lastName;
          this.mobileNo = data.mobileNo;
          this.qualification = data.qualification || '';
          this.licenceNumber = data.licenceNumber || '';
          this.yearsOfExperience = data.yearsOfExperience || 0;
          this.consultationFee = data.consultationFee || 0;
          this.about = data.aboutDoctor || data.about || '';
          this.specializationId = data.specializationId || '';

          // Address
          this.country = data.country || 'India';
          this.state = data.state || '';
          this.city = data.city || '';
          this.area = data.area || '';
          this.pincode = data.pincode || '';
          this.addressline1 = data.addressline1 || '';
          this.addressline2 = data.addressline2 || '';

          if (form && form.control) {
            form.control.markAsPristine();
            form.control.markAsUntouched();
          }
        },
        error: () => {
          this.toastService.showError('Failed to load doctor profile.');
        }
      });
    } else if (this.role === 'Admin') {
      this.patientService.getAdminProfile().subscribe({
        next: (data: any) => {
          this.firstName = data.firstName;
          this.lastName = data.lastName;
          this.mobileNo = data.mobileNo;
          this.clinicName = data.clinicName || 'N/A';

          // Address
          this.country = data.country || 'India';
          this.state = data.state || '';
          this.city = data.city || '';
          this.area = data.area || '';
          this.pincode = data.pincode || '';
          this.addressline1 = data.addressline1 || '';
          this.addressline2 = data.addressline2 || '';

          if (form && form.control) {
            form.control.markAsPristine();
            form.control.markAsUntouched();
          }
        },
        error: () => {
          this.toastService.showError('Failed to load clinic admin profile.');
        }
      });
    }
  }

  onSubmit(form: any): void {
    this.backendErrors = {};
    if (form.invalid) {
      Object.keys(form.controls).forEach(key => {
        form.controls[key].markAsTouched();
      });
      this.toastService.showError('Please complete all required fields correctly.');
      return;
    }

    let request$;

    if (this.role === 'Patient') {
      const payload = {
        firstName: this.firstName,
        lastName: this.lastName,
        mobileNo: this.mobileNo,
        gender: this.gender,
        dob: this.dob,
        bloodGroup: this.bloodGroup,
        emergencyContactName: this.emergencyContactName ? this.emergencyContactName : null,
        emergencyContactNumber: this.emergencyContactNumber ? this.emergencyContactNumber : null,
        country: this.country,
        state: this.state,
        city: this.city,
        area: this.area,
        pincode: this.pincode,
        addressline1: this.addressline1,
        addressline2: this.addressline2
      };
      request$ = this.patientService.updatePatientProfile(this.profileId, payload);
    } else if (this.role === 'Doctor') {
      const payload = {
        firstName: this.firstName,
        lastName: this.lastName,
        mobileNo: this.mobileNo,
        qualification: this.qualification,
        licenceNumber: this.licenceNumber,
        yearsOfExperience: this.yearsOfExperience,
        consultationFee: this.consultationFee,
        aboutDoctor: this.about,
        specializationId: this.specializationId,
        country: this.country,
        state: this.state,
        city: this.city,
        area: this.area,
        pincode: this.pincode,
        addressline1: this.addressline1,
        addressline2: this.addressline2
      };
      request$ = this.patientService.updateDoctorProfile(payload);
    } else {
      const payload = {
        firstName: this.firstName,
        lastName: this.lastName,
        mobileNo: this.mobileNo,
        country: this.country,
        state: this.state,
        city: this.city,
        area: this.area,
        pincode: this.pincode,
        addressline1: this.addressline1,
        addressline2: this.addressline2
      };
      request$ = this.patientService.updateAdminProfile(payload);
    }

    request$.subscribe({
      next: (updatedProfile: any) => {
        if (form && form.control) {
          form.control.markAsPristine();
        }

        sessionStorage.setItem('firstName', updatedProfile.firstName);
        sessionStorage.setItem('lastName', updatedProfile.lastName);

        if (this.role === 'Patient') {
          this.completionStats = this.calculateStats(updatedProfile);
          sessionStorage.setItem('profileCompletion', this.completionStats.percentage.toString());
        }
        
        this.toastService.showSuccess('Profile updated successfully!');
      },
      error: (err: any) => {
        if (err?.error?.errors) {
          Object.keys(err.error.errors).forEach(field => {
            const errorMsg = err.error.errors[field][0];
            this.backendErrors[field] = errorMsg;
            const camelKey = field.charAt(0).toLowerCase() + field.slice(1);
            this.backendErrors[camelKey] = errorMsg;
          });
          this.toastService.showError('Please fix the highlighted fields with validation errors.');
        } else {
          this.toastService.showError(err?.error?.detail || 'Failed to update profile details.');
        }
      }
    });
  }

  calculateStats(data: any): { percentage: number; left: string[] } {
    let completed = 0;
    const left: string[] = [];

    if (data.firstName && data.firstName.trim()) completed += 15; else left.push('First Name');
    if (data.lastName && data.lastName.trim()) completed += 15; else left.push('Last Name');
    if (data.mobileNo && data.mobileNo.trim()) completed += 15; else left.push('Mobile Number');
    if (data.gender) completed += 15; else left.push('Gender Selection');
    if (data.dob && data.dob !== '0001-01-01') completed += 15; else left.push('Date of Birth');
    if (data.bloodGroup) completed += 15; else left.push('Blood Group');

    if (data.emergencyContactName && data.emergencyContactName.trim() && 
        data.emergencyContactNumber && data.emergencyContactNumber.trim()) {
      completed += 10;
    } else {
      left.push('Emergency Contact Details');
    }

    return { percentage: Math.min(completed, 100), left };
  }
}
