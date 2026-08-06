import { Component, OnInit, OnDestroy } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { PatientService } from '../../core/services/patient.service';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { AppointmentService } from '../../core/services/appointment.service';
import { NotificationService } from '../../core/services/notification.service';

@Component({
  selector: 'app-profile',
  templateUrl: './profile.component.html',
  styleUrls: ['./profile.component.css']
})
export class ProfileComponent implements OnInit, OnDestroy {
  profileId = '';
  role = '';
  
  // Demographics Fields
  firstName = '';
  lastName = '';
  email = '';
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
  verificationStatus = 'Approved';
  rejectionReason = '';
  languagesSpoken = 'English, Hindi, Gujarati';

  // Admin Fields
  clinicName = '';

  // Backend validation errors mapping
  backendErrors: { [key: string]: string } = {};

  // Decoupled Saved Stats State
  completionStats = { percentage: 30, left: [] as string[] };

  errorMessage = '';
  successMessage = '';

  // Password Update Fields
  showChangePassword = false;
  changePasswordStep: 'init' | 'otp' = 'init';
  currentPassword = '';
  changeOtp = '';
  changeNewPassword = '';
  changeConfirmPassword = '';
  isChangingPassword = false;
  showChangePasswordToggle = false;
  showChangeConfirmPasswordToggle = false;

  // Contact Info Update Fields (Email & WhatsApp Number)
  updateType: 'whatsapp' | 'email' = 'whatsapp';
  showContactModal = false;
  contactStep: 'input' | 'otp' = 'input';
  newEmail = '';
  newMobileNo = '';
  emailOtp = '';
  mobileOtp = '';
  isSubmittingContact = false;

  resendCooldown = 0;
  cooldownInterval: any;

  constructor(
    private patientService: PatientService,
    public authService: AuthService,
    private toastService: ToastService,
    private appointmentService: AppointmentService,
    private notificationService: NotificationService,
    private route: ActivatedRoute
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
    this.role = this.authService.getRole() || '';
    const activeUser = this.authService.getActiveUserForCurrentRoute();
    this.profileId = sessionStorage.getItem('profileId') || activeUser?.patientId || activeUser?.userId || '';
    this.loadProfile();

    if (this.role === 'Doctor') {
      this.loadSpecializations();
    }

    this.route.queryParams.subscribe(params => {
      if (params['action'] === 'change-password') {
        this.showChangePassword = true;
      }
    });
  }

  loadSpecializations(): void {
    this.appointmentService.getSpecializations().subscribe({
      next: (data) => {
        this.specializations = data;
      }
    });
  }

  loadProfile(form?: any): void {
    if (this.role === 'Patient') {
      this.patientService.getPatientSelfProfile().subscribe({
        next: (data: any) => {
          this.firstName = data.firstName;
          this.lastName = data.lastName;
          this.email = data.email || this.authService.getAnyActiveUser()?.email || '';
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

          if (data.patientId) {
            sessionStorage.setItem('profileId', data.patientId);
          }

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
          this.gender = data.gender || 'Male';
          this.dob = data.dob ? data.dob.split('T')[0] : '';
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
          this.gender = data.gender || 'Male';
          this.dob = data.dob ? data.dob.split('T')[0] : '';
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
      request$ = this.patientService.updatePatientSelfProfile(payload);
    } else if (this.role === 'Doctor') {
      const payload = {
        firstName: this.firstName,
        lastName: this.lastName,
        mobileNo: this.mobileNo,
        gender: this.gender,
        dob: this.dob,
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
        gender: this.gender,
        dob: this.dob,
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
        
        this.notificationService.notifyDataRefresh('Profile');
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

    const fName = this.firstName || data?.firstName || '';
    const lName = this.lastName || data?.lastName || '';
    const mob = this.mobileNo || data?.mobileNo || data?.mobile || '';
    const gen = this.gender || data?.gender || '';
    const birthDate = this.dob || (data?.dob ? data.dob.split('T')[0] : '');
    const bGroup = this.bloodGroup || data?.bloodGroup || '';
    const emName = this.emergencyContactName || data?.emergencyContactName || '';
    const emNum = this.emergencyContactNumber || data?.emergencyContactNumber || '';

    if (fName && fName.trim()) completed += 15; else left.push('First Name');
    if (lName && lName.trim()) completed += 15; else left.push('Last Name');
    if (mob && mob.trim() && mob !== 'Not Added') completed += 15; else left.push('Mobile Number');
    if (gen) completed += 15; else left.push('Gender Selection');
    if (birthDate && birthDate !== '0001-01-01' && birthDate !== '0001-01-01T00:00:00') completed += 15; else left.push('Date of Birth');
    if (bGroup) completed += 15; else left.push('Blood Group');

    if (emName && emName.trim() && emNum && emNum.trim()) {
      completed += 10;
    } else {
      left.push('Emergency Contact Details');
    }

    return { percentage: Math.min(completed, 100), left };
  }

  toggleChangePasswordSection(): void {
    this.showChangePassword = !this.showChangePassword;
    this.changePasswordStep = 'init';
    this.currentPassword = '';
    this.changeOtp = '';
    this.changeNewPassword = '';
    this.changeConfirmPassword = '';
  }

  onInitiatePasswordUpdate(): void {
    if (!this.currentPassword) {
      this.toastService.showError('Please enter your current password.');
      return;
    }
    if (!this.changeNewPassword || this.changeNewPassword.length < 6) {
      this.toastService.showError('New password must be at least 6 characters long.');
      return;
    }
    if (this.changeNewPassword !== this.changeConfirmPassword) {
      this.toastService.showError('Passwords do not match.');
      return;
    }

    this.isChangingPassword = true;
    this.authService.initiatePasswordUpdate(this.currentPassword).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message || 'Verification OTP sent to your email.');
        this.changePasswordStep = 'otp';
        this.isChangingPassword = false;
        this.startCooldown();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Current password verification failed.');
        this.isChangingPassword = false;
      }
    });
  }

  onConfirmPasswordUpdate(): void {
    if (!this.changeOtp || this.changeOtp.length !== 6) {
      this.toastService.showError('Please enter a valid 6-digit OTP code.');
      return;
    }

    this.isChangingPassword = true;
    this.authService.updatePassword(this.changeOtp, this.changeNewPassword).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message || 'Password changed successfully!');
        this.toggleChangePasswordSection();
        this.isChangingPassword = false;
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to update password. Please check your OTP.');
        this.isChangingPassword = false;
      }
    });
  }

  resendChangePasswordOtp(): void {
    if (this.resendCooldown > 0) return;
    this.isChangingPassword = true;
    this.authService.initiatePasswordUpdate(this.currentPassword).subscribe({
      next: (res) => {
        this.toastService.showSuccess('A new OTP has been sent successfully!');
        this.isChangingPassword = false;
        this.startCooldown();
      },
      error: (err) => {
        this.toastService.showError(err, 'Failed to resend OTP.');
        this.isChangingPassword = false;
      }
    });
  }

  mobileValidationError: string = '';

  /*
  ================================================================================
  🌍 FUTURE MULTI-COUNTRY CODE EXPANSION MATRIX (PRESERVED FOR FUTURE USE)
  ================================================================================
  public readonly INTERNATIONAL_COUNTRY_CODES = [
    { code: '+91',  name: 'India', flag: '🇮🇳', length: 10, pattern: /^[6-9]\d{9}$/ },
    { code: '+1',   name: 'United States / Canada', flag: '🇺🇸', length: 10, pattern: /^[2-9]\d{9}$/ },
    { code: '+44',  name: 'United Kingdom', flag: '🇬🇧', length: 10, pattern: /^7\d{9}$/ },
    { code: '+971', name: 'United Arab Emirates', flag: '🇦🇪', length: 9, pattern: /^5\d{8}$/ },
    { code: '+61',  name: 'Australia', flag: '🇦🇺', length: 9, pattern: /^4\d{8}$/ },
    { code: '+49',  name: 'Germany', flag: '🇩🇪', length: 10, pattern: /^1[5-7]\d{8,9}$/ },
    { code: '+65',  name: 'Singapore', flag: '🇸🇬', length: 8, pattern: /^[89]\d{7}$/ },
    { code: '+966', name: 'Saudi Arabia', flag: '🇸🇦', length: 9, pattern: /^5\d{8}$/ }
  ];
  ================================================================================
  */

  onMobileInputChanged(): void {
    if (!this.newMobileNo) {
      this.mobileValidationError = '';
      return;
    }

    // Auto-strip non-digits
    this.newMobileNo = this.newMobileNo.replace(/\D/g, '');

    if (this.newMobileNo.length > 0 && !/^[6-9]/.test(this.newMobileNo)) {
      this.mobileValidationError = 'Indian mobile number must start with 6, 7, 8, or 9.';
    } else if (this.newMobileNo.length > 0 && this.newMobileNo.length < 10) {
      this.mobileValidationError = `Please enter remaining ${10 - this.newMobileNo.length} digit(s) (10 digits required).`;
    } else {
      this.mobileValidationError = '';
    }
  }

  openContactModal(): void {
    this.openWhatsAppModal();
  }

  openWhatsAppModal(): void {
    this.updateType = 'whatsapp';
    this.showContactModal = true;
    this.contactStep = 'input';
    this.newEmail = '';
    this.newMobileNo = '';
    this.emailOtp = '';
    this.mobileOtp = '';
    this.mobileValidationError = '';
  }

  openEmailModal(): void {
    this.updateType = 'email';
    this.showContactModal = true;
    this.contactStep = 'input';
    this.newEmail = '';
    this.newMobileNo = '';
    this.emailOtp = '';
    this.mobileOtp = '';
    this.mobileValidationError = '';
  }

  closeContactModal(): void {
    this.showContactModal = false;
    this.mobileValidationError = '';
  }

  onInitiateContactUpdate(): void {
    if (this.updateType === 'whatsapp') {
      if (!this.newMobileNo || !this.newMobileNo.trim()) {
        this.mobileValidationError = 'Please enter a 10-digit WhatsApp mobile number.';
        return;
      }

      const cleanNum = this.newMobileNo.trim().replace(/\D/g, '');

      if (cleanNum.length !== 10) {
        this.mobileValidationError = 'Mobile number must be exactly 10 digits long.';
        return;
      }

      if (!/^[6-9]\d{9}$/.test(cleanNum)) {
        this.mobileValidationError = 'Invalid Indian mobile number. Must start with 6, 7, 8, or 9.';
        return;
      }

      const existingClean = (this.mobileNo || '').replace(/\D/g, '');
      if (existingClean && cleanNum && (cleanNum === existingClean || cleanNum.endsWith(existingClean) || existingClean.endsWith(cleanNum))) {
        this.toastService.showError('Please enter a different WhatsApp number to update.');
        return;
      }
    } else {
      if (!this.newEmail || !this.newEmail.trim()) {
        this.toastService.showError('Please enter a new Email address.');
        return;
      }
    }

    this.isSubmittingContact = true;
    this.authService.initiateContactUpdate({
      newEmail: this.updateType === 'email' ? this.newEmail.trim() : '',
      newMobileNo: this.updateType === 'whatsapp' ? this.newMobileNo.trim() : ''
    }).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message || 'Verification OTP sent!');
        this.contactStep = 'otp';
        this.isSubmittingContact = false;
      },
      error: (err) => {
        this.toastService.showError(err, 'Failed to initiate contact update.');
        this.isSubmittingContact = false;
      }
    });
  }

  onConfirmContactUpdate(): void {
    if (this.updateType === 'whatsapp' && (!this.mobileOtp || this.mobileOtp.trim().length !== 6)) {
      this.toastService.showError('Please enter valid 6-digit WhatsApp OTP code.');
      return;
    }
    if (this.updateType === 'email' && (!this.emailOtp || this.emailOtp.trim().length !== 6)) {
      this.toastService.showError('Please enter valid 6-digit Email OTP code.');
      return;
    }

    this.isSubmittingContact = true;
    this.authService.confirmContactUpdate({
      newEmail: this.updateType === 'email' ? this.newEmail.trim() : '',
      newMobileNo: this.updateType === 'whatsapp' ? this.newMobileNo.trim() : '',
      emailOtp: this.updateType === 'email' ? this.emailOtp.trim() : '',
      mobileOtp: this.updateType === 'whatsapp' ? this.mobileOtp.trim() : ''
    }).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message || 'Contact detail updated successfully!');
        this.closeContactModal();
        this.isSubmittingContact = false;
        if (this.updateType === 'email' && res.email) this.email = res.email;
        if (this.updateType === 'whatsapp' && res.mobileNo) this.mobileNo = res.mobileNo;

        // Recalculate profile completion after successful update
        if (this.role === 'Patient') {
          this.completionStats = this.calculateStats({});
          sessionStorage.setItem('profileCompletion', this.completionStats.percentage.toString());
        }
        this.notificationService.notifyDataRefresh('Profile');
      },
      error: (err) => {
        this.toastService.showError(err, 'Verification failed. Please check OTP code.');
        this.isSubmittingContact = false;
      }
    });
  }
}
