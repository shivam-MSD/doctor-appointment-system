import { Component, OnInit, OnDestroy } from '@angular/core';
import { AdminService } from '../../../core/services/admin.service';
import { ToastService } from '../../../core/services/toast.service';
import { NotificationService } from '../../../core/services/notification.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-super-admin-dashboard',
  templateUrl: './super-admin-dashboard.component.html',
  styleUrls: ['./super-admin-dashboard.component.css']
})
export class SuperAdminDashboardComponent implements OnInit, OnDestroy {
  pendingDoctors: any[] = [];
  pendingClinics: any[] = [];
  pendingAdmins: any[] = [];
  errorMessage = '';
  successMessage = '';
  isSuperAdminLoading = true;
  private signalrSub?: Subscription;

  // Reject clinic states
  showRejectModal = false;
  selectedClinicIdForRejection = '';
  rejectionReason = '';

  constructor(
    private adminService: AdminService,
    private toastService: ToastService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadPendingRequests();

    // Auto-reload the dashboard pending queues in real-time when refresh signals are received
    this.signalrSub = this.notificationService.refreshData$.subscribe({
      next: (area) => {
        if (area === 'Clinics' || area === 'Doctors' || area === 'Admins') {
          this.loadPendingRequests();
        }
      }
    });
  }

  ngOnDestroy(): void {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
  }

  loadPendingRequests(): void {
    this.isSuperAdminLoading = true;
    this.adminService.getPendingDoctors().subscribe({
      next: (res) => this.pendingDoctors = res,
      error: () => this.errorMessage = 'Failed to load pending doctors.'
    });

    this.adminService.getPendingClinics().subscribe({
      next: (res) => this.pendingClinics = res,
      error: () => this.errorMessage = 'Failed to load pending clinics.'
    });

    this.adminService.getPendingAdmins().subscribe({
      next: (res) => {
        this.pendingAdmins = res;
        this.isSuperAdminLoading = false;
      },
      error: () => {
        this.errorMessage = 'Failed to load pending admins.';
        this.isSuperAdminLoading = false;
      }
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

  rejectDoctor(doctorUserId: string): void {
    this.adminService.rejectDoctor(doctorUserId).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message || 'Doctor rejected successfully.');
        this.loadPendingRequests();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to reject doctor.');
      }
    });
  }

  rejectAdmin(adminId: string): void {
    this.adminService.rejectAdmin(adminId).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message || 'Clinic Admin rejected successfully.');
        this.loadPendingRequests();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to reject admin.');
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
