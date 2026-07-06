import { Component, OnInit } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { AppointmentService } from '../../core/services/appointment.service';
import { AdminService } from '../../core/services/admin.service';
import { PatientService } from '../../core/services/patient.service';
import { ToastService } from '../../core/services/toast.service';
import { Appointment } from '../../core/models/appointment.model';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit {
  role = '';
  appointments: Appointment[] = [];
  totalCount = 0;
  statusFilter = '';
  firstName = '';
  errorMessage = '';

  // Doctor completeness state
  isDoctorAddressIncomplete = false;

  // SuperAdmin lists
  pendingDoctors: any[] = [];
  pendingClinics: any[] = [];
  pendingAdmins: any[] = [];

  // Doctor lists & states
  doctorClinics: any[] = [];
  selectedClinicIds: string[] = [];
  showClinicModal = false;
  showAdminModal = false;
  selectedClinicIdForAdmin = '';
  selectedClinicNameForAdmin = '';

  // Reject clinic states
  showRejectModal = false;
  selectedClinicIdForRejection = '';
  rejectionReason = '';

  // Edit clinic states
  showEditClinicModal = false;
  selectedClinicIdForEdit = '';
  clinicEditForm = {
    clinicName: '',
    clinicType: 'Clinic',
    country: 'India',
    state: '',
    city: '',
    area: '',
    pincode: '',
    addressline1: '',
    addressline2: ''
  };

  clinicOnlyForm = {
    clinicName: '',
    clinicType: 'Clinic',
    country: 'India',
    state: '',
    city: '',
    area: '',
    pincode: '',
    addressline1: '',
    addressline2: ''
  };

  adminForm = {
    clinicId: '',
    adminEmail: '',
    adminPassword: '',
    adminFirstName: '',
    adminLastName: '',
    adminMobileNo: ''
  };

  constructor(
    private authService: AuthService,
    private appointmentService: AppointmentService,
    private adminService: AdminService,
    private patientService: PatientService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.role = this.authService.getRole() || 'Patient';
    this.firstName = sessionStorage.getItem('firstName') || 'User';
    this.loadDashboardData();
  }

  loadDashboardData(): void {
    if (this.role === 'Patient') {
      this.appointmentService.getPatientDashboard(this.statusFilter, 1, 10).subscribe({
        next: (res) => {
          this.appointments = res.items;
          this.totalCount = res.totalCount;
        },
        error: () => {
          this.errorMessage = 'Failed to load patient appointments.';
        }
      });
    } else if (this.role === 'SuperAdmin') {
      this.loadSuperAdminData();
    } else {
      // Doctor or Clinic Admin
      this.appointmentService.getAdminDoctorDashboard({ status: this.statusFilter }, 1, 10).subscribe({
        next: (res) => {
          this.appointments = res.items;
          this.totalCount = res.totalCount;
        },
        error: () => {
          this.errorMessage = 'Failed to load dashboard appointments.';
        }
      });

      if (this.role === 'Doctor') {
        this.loadDoctorClinics();
        this.checkDoctorProfileCompleteness();
      }
    }
  }

  checkDoctorProfileCompleteness(): void {
    this.patientService.getDoctorProfile().subscribe({
      next: (profile) => {
        // If state, city, pincode, or addressline1 are blank/empty, flag it as incomplete!
        if (!profile.state || !profile.city || !profile.pincode || !profile.addressline1) {
          this.isDoctorAddressIncomplete = true;
        }
      }
    });
  }

  loadSuperAdminData(): void {
    this.adminService.getPendingDoctors().subscribe({
      next: (res) => this.pendingDoctors = res,
      error: () => this.errorMessage = 'Failed to load pending doctors.'
    });

    this.adminService.getPendingClinics().subscribe({
      next: (res) => this.pendingClinics = res,
      error: () => this.errorMessage = 'Failed to load pending clinics.'
    });

    this.adminService.getPendingAdmins().subscribe({
      next: (res) => this.pendingAdmins = res,
      error: () => this.errorMessage = 'Failed to load pending admins.'
    });
  }

  loadDoctorClinics(): void {
    this.adminService.getDoctorClinics().subscribe({
      next: (res) => this.doctorClinics = res,
      error: () => {}
    });
  }

  // SuperAdmin Verification Actions
  verifyDoctor(doctorId: string, status: string): void {
    this.adminService.verifyDoctor(doctorId, status).subscribe({
      next: () => {
        this.toastService.showSuccess(`Doctor verification status updated to '${status}'.`);
        this.loadSuperAdminData();
      },
      error: (err) => this.toastService.showError(err?.error?.detail || 'Failed to verify doctor.')
    });
  }

  verifyClinic(clinicId: string): void {
    this.adminService.verifyClinic(clinicId).subscribe({
      next: () => {
        this.toastService.showSuccess('Clinic verified successfully.');
        this.loadSuperAdminData();
      },
      error: (err) => this.toastService.showError(err?.error?.detail || 'Failed to verify clinic.')
    });
  }

  verifyAdmin(adminId: string): void {
    this.adminService.verifyAdmin(adminId).subscribe({
      next: () => {
        this.toastService.showSuccess('Clinic Admin verified successfully.');
        this.loadSuperAdminData();
      },
      error: (err) => this.toastService.showError(err?.error?.detail || 'Failed to verify clinic admin.')
    });
  }

  // Doctor Clinic Registration Action
  openClinicModal(): void {
    this.showClinicModal = true;
    this.errorMessage = '';
    this.clinicOnlyForm = {
      clinicName: '',
      clinicType: 'Clinic',
      country: 'India',
      state: '',
      city: '',
      area: '',
      pincode: '',
      addressline1: '',
      addressline2: ''
    };
  }

  closeClinicModal(): void {
    this.showClinicModal = false;
  }

  submitClinicRegistration(): void {
    this.adminService.registerClinicOnly(this.clinicOnlyForm).subscribe({
      next: () => {
        this.toastService.showSuccess('Clinic registered successfully. Awaiting Super Admin verification.');
        this.closeClinicModal();
        this.loadDoctorClinics();
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail || 'Failed to register clinic.';
        this.toastService.showError(this.errorMessage);
      }
    });
  }

  // Doctor Admin Registration Action
  openAdminModal(clinicId: string, clinicName: string): void {
    this.showAdminModal = true;
    this.errorMessage = '';
    this.selectedClinicIdForAdmin = clinicId;
    this.selectedClinicNameForAdmin = clinicName;
    this.adminForm = {
      clinicId: clinicId,
      adminEmail: '',
      adminPassword: '',
      adminFirstName: '',
      adminLastName: '',
      adminMobileNo: ''
    };
  }

  closeAdminModal(): void {
    this.showAdminModal = false;
  }

  submitAdminRegistration(): void {
    if (!this.adminForm.clinicId) {
      this.errorMessage = 'Please select a clinic.';
      this.toastService.showError(this.errorMessage);
      return;
    }
    this.adminService.registerClinicAdmin(this.adminForm).subscribe({
      next: () => {
        this.toastService.showSuccess('Clinic Admin registered successfully. Awaiting Super Admin verification.');
        this.closeAdminModal();
        this.loadDoctorClinics();
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail || 'Failed to register clinic admin.';
        this.toastService.showError(this.errorMessage);
      }
    });
  }

  getVerifiedClinicsWithoutAdmin(): any[] {
    return this.doctorClinics.filter(c => c.isVerified && !c.hasAdmin);
  }

  onFilterChange(status: string): void {
    this.statusFilter = status;
    this.loadDashboardData();
  }

  cancelAppointment(id: string): void {
    if (confirm('Are you sure you want to cancel this appointment?')) {
      this.appointmentService.cancelAppointment(id).subscribe({
        next: () => {
          this.toastService.showSuccess('Appointment cancelled successfully.');
          this.loadDashboardData();
        },
        error: (err) => {
          this.toastService.showError(err?.error?.detail || 'Failed to cancel appointment.');
        }
      });
    }
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Pending': return 'badge badge-pending';
      case 'Confirmed': return 'badge badge-confirmed';
      case 'Cancelled': return 'badge badge-cancelled';
      case 'Completed': return 'badge badge-completed';
      default: return 'badge';
    }
  }

  toggleClinicFilter(clinicId: string): void {
    const idx = this.selectedClinicIds.indexOf(clinicId);
    if (idx > -1) {
      this.selectedClinicIds.splice(idx, 1);
    } else {
      this.selectedClinicIds.push(clinicId);
    }
  }

  getFilteredAppointments(): Appointment[] {
    if (!this.appointments) return [];
    if (this.role !== 'Doctor' || this.selectedClinicIds.length === 0) {
      return this.appointments;
    }
    return this.appointments.filter(app => app.clinicId && this.selectedClinicIds.includes(app.clinicId));
  }

  // Reject clinic methods
  openRejectClinicModal(clinicId: string): void {
    this.selectedClinicIdForRejection = clinicId;
    this.rejectionReason = '';
    this.showRejectModal = true;
  }

  closeRejectModal(): void {
    this.showRejectModal = false;
    this.selectedClinicIdForRejection = '';
    this.rejectionReason = '';
  }

  submitClinicRejection(): void {
    if (!this.selectedClinicIdForRejection || !this.rejectionReason.trim()) {
      this.toastService.showError('Please enter a rejection reason.');
      return;
    }

    this.adminService.rejectClinic(this.selectedClinicIdForRejection, this.rejectionReason).subscribe({
      next: () => {
        this.toastService.showSuccess('Clinic registration rejected successfully.');
        this.closeRejectModal();
        this.loadSuperAdminData();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to reject clinic.');
      }
    });
  }

  // Edit clinic methods
  openEditClinicModal(clinic: any): void {
    this.selectedClinicIdForEdit = clinic.clinicId;
    this.clinicEditForm = {
      clinicName: clinic.clinicName,
      clinicType: clinic.clinicType,
      country: 'India', // Default to India
      state: clinic.state,
      city: clinic.city,
      area: clinic.area || '',
      pincode: clinic.pincode || '',
      addressline1: clinic.addressline1 || '',
      addressline2: clinic.addressline2 || ''
    };
    this.showEditClinicModal = true;
  }

  closeEditClinicModal(): void {
    this.showEditClinicModal = false;
    this.selectedClinicIdForEdit = '';
  }

  submitClinicEdit(): void {
    if (!this.selectedClinicIdForEdit) return;

    this.adminService.updateClinic(this.selectedClinicIdForEdit, this.clinicEditForm).subscribe({
      next: () => {
        this.toastService.showSuccess('Clinic details updated. Awaiting Super Admin verification.');
        this.closeEditClinicModal();
        this.loadDoctorClinics();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to update clinic details.');
      }
    });
  }
}
