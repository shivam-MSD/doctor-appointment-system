import { Component, OnInit } from '@angular/core';
import { AdminService } from '../../../core/services/admin.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-super-admin-dashboard',
  templateUrl: './super-admin-dashboard.component.html',
  styleUrls: ['./super-admin-dashboard.component.css']
})
export class SuperAdminDashboardComponent implements OnInit {
  pendingDoctors: any[] = [];
  pendingClinics: any[] = [];
  pendingAdmins: any[] = [];
  errorMessage = '';
  successMessage = '';

  // Reject clinic states
  showRejectModal = false;
  selectedClinicIdForRejection = '';
  rejectionReason = '';

  constructor(
    private adminService: AdminService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.loadPendingRequests();
  }

  loadPendingRequests(): void {
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

  approveDoctor(doctorUserId: string): void {
    this.adminService.approveDoctor(doctorUserId).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message || 'Doctor approved successfully!');
        this.loadPendingRequests();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to approve doctor.');
      }
    });
  }

  verifyClinic(clinicId: string): void {
    this.adminService.verifyClinic(clinicId).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message || 'Clinic verified successfully!');
        this.loadPendingRequests();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to verify clinic.');
      }
    });
  }

  verifyAdmin(adminId: string): void {
    this.adminService.verifyAdmin(adminId).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message || 'Clinic Admin approved successfully!');
        this.loadPendingRequests();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to approve admin.');
      }
    });
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
        this.loadPendingRequests();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to reject clinic.');
      }
    });
  }
}
