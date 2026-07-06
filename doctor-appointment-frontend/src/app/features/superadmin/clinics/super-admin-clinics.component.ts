import { Component, OnInit, OnDestroy } from '@angular/core';
import { AdminService } from '../../../core/services/admin.service';
import { ToastService } from '../../../core/services/toast.service';
import { NotificationService } from '../../../core/services/notification.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-super-admin-clinics',
  templateUrl: './super-admin-clinics.component.html',
  styleUrls: ['./super-admin-clinics.component.css']
})
export class SuperAdminClinicsComponent implements OnInit, OnDestroy {
  clinics: any[] = [];
  searchQuery = '';
  stateFilter = '';
  cityFilter = '';
  verifiedFilter: boolean | undefined = undefined;
  errorMessage = '';
  successMessage = '';
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
    this.loadClinics();

    // Auto-reload the list in real-time when silent refresh signals are received
    this.signalrSub = this.notificationService.refreshData$.subscribe({
      next: (area) => {
        if (area === 'Clinics') {
          this.loadClinics();
        }
      }
    });
  }

  ngOnDestroy(): void {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
  }

  loadClinics(): void {
    this.adminService.getAllClinics(this.searchQuery, this.stateFilter, this.cityFilter, this.verifiedFilter).subscribe({
      next: (res) => {
        this.clinics = res;
      },
      error: () => {
        this.toastService.showError('Failed to load clinics list.');
      }
    });
  }

  verifyClinic(clinicId: string): void {
    this.adminService.verifyClinic(clinicId).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message || 'Clinic branch verified successfully!');
        this.loadClinics();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to verify clinic branch.');
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
        this.loadClinics();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to reject clinic.');
      }
    });
  }
}
