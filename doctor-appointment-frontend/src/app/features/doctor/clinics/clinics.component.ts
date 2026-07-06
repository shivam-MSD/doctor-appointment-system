import { Component, OnInit, OnDestroy } from '@angular/core';
import { AdminService } from '../../../core/services/admin.service';
import { ToastService } from '../../../core/services/toast.service';
import { NotificationService } from '../../../core/services/notification.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-clinics',
  templateUrl: './clinics.component.html',
  styleUrls: ['./clinics.component.css']
})
export class ClinicsComponent implements OnInit, OnDestroy {
  doctorClinics: any[] = [];
  errorMessage = '';
  successMessage = '';
  showClinicModal = false;
  showAdminModal = false;
  selectedClinicIdForAdmin = '';
  selectedClinicNameForAdmin = '';
  private signalrSub?: Subscription;

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
    private adminService: AdminService,
    private toastService: ToastService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadDoctorClinics();

    // Auto-reload doctor clinics table in real-time when refresh signals are received
    this.signalrSub = this.notificationService.refreshData$.subscribe({
      next: (area) => {
        if (area === 'Clinics') {
          this.loadDoctorClinics();
        }
      }
    });
  }

  ngOnDestroy(): void {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
  }

  loadDoctorClinics(): void {
    this.adminService.getDoctorClinics().subscribe({
      next: (res) => {
        this.doctorClinics = res;
      },
      error: () => {
        this.errorMessage = 'Failed to load clinic locations.';
      }
    });
  }

  openClinicModal(): void {
    this.showClinicModal = true;
    this.errorMessage = '';
    this.successMessage = '';
  }

  closeClinicModal(): void {
    this.showClinicModal = false;
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

  openAdminModal(clinicId: string, clinicName: string): void {
    this.selectedClinicIdForAdmin = clinicId;
    this.selectedClinicNameForAdmin = clinicName;
    this.adminForm.clinicId = clinicId;
    this.showAdminModal = true;
    this.errorMessage = '';
    this.successMessage = '';
  }

  closeAdminModal(): void {
    this.showAdminModal = false;
    this.adminForm = {
      clinicId: '',
      adminEmail: '',
      adminPassword: '',
      adminFirstName: '',
      adminLastName: '',
      adminMobileNo: ''
    };
  }

  submitClinicOnly(): void {
    this.adminService.registerClinicOnly(this.clinicOnlyForm).subscribe({
      next: (res) => {
        this.successMessage = res.message || 'Clinic registered successfully! Pending Super Admin verification.';
        this.toastService.showSuccess(this.successMessage);
        this.loadDoctorClinics();
        setTimeout(() => this.closeClinicModal(), 2000);
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail || 'Failed to register clinic.';
        this.toastService.showError(this.errorMessage);
      }
    });
  }

  submitAdmin(): void {
    this.adminService.registerClinicAdmin(this.adminForm).subscribe({
      next: (res) => {
        this.successMessage = res.message || 'Clinic Admin registered successfully! Pending Super Admin verification.';
        this.toastService.showSuccess(this.successMessage);
        this.loadDoctorClinics();
        setTimeout(() => this.closeAdminModal(), 2000);
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail || 'Failed to register clinic admin.';
        this.toastService.showError(this.errorMessage);
      }
    });
  }

  // Edit clinic methods
  openEditClinicModal(clinic: any): void {
    this.selectedClinicIdForEdit = clinic.clinicId;
    this.clinicEditForm = {
      clinicName: clinic.clinicName,
      clinicType: clinic.clinicType,
      country: 'India',
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
