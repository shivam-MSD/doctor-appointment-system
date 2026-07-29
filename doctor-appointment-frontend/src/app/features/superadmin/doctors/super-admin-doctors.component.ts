import { Component, OnInit, OnDestroy } from '@angular/core';
import { AdminService } from '../../../core/services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { ToastService } from '../../../core/services/toast.service';
import { Subscription } from 'rxjs';

@Component({
	selector: 'app-super-admin-doctors',
	templateUrl: './super-admin-doctors.component.html',
	styleUrls: ['./super-admin-doctors.component.css']
})
export class SuperAdminDoctorsComponent implements OnInit, OnDestroy {
	doctors: any[] = [];
	searchQuery = '';
	statusFilter = ''; // Empty for all, Verified, Pending
	errorMessage = '';
	successMessage = '';
	isDoctorsLoading = true;
	private signalrSub?: Subscription;

	// View Details modal states
	showDetailsModal = false;
	selectedDoctorForDetails: any = null;
	isDetailsVerified = false;

	constructor(
		private adminService: AdminService,
		private notificationService: NotificationService,
		private toastService: ToastService
	) {}

  ngOnInit(): void {
    this.loadDoctors();

    // Auto-reload the doctor application roster in real-time when refresh signals are received
    this.signalrSub = this.notificationService.refreshData$.subscribe({
      next: (area) => {
        if (area === 'Doctors') {
          this.loadDoctors();
        }
      }
    });
  }

  ngOnDestroy(): void {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
  }

  loadDoctors(): void {
    this.isDoctorsLoading = true;
    this.adminService.getAllDoctors(this.searchQuery, this.statusFilter).subscribe({
      next: (res) => {
        this.doctors = res;
        this.isDoctorsLoading = false;
      },
      error: () => {
        this.errorMessage = 'Failed to load doctors list.';
        this.isDoctorsLoading = false;
      }
    });
  }

  openDetailsModal(doctor: any): void {
    this.selectedDoctorForDetails = doctor;
    this.isDetailsVerified = false;
    this.showDetailsModal = true;
  }

  closeDetailsModal(): void {
    this.showDetailsModal = false;
    this.selectedDoctorForDetails = null;
    this.isDetailsVerified = false;
  }

  approveDoctor(doctorUserId: string): void {
    this.adminService.approveDoctor(doctorUserId).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message || 'Doctor approved successfully!');
        this.loadDoctors();
      },
      error: (err) => {
        this.toastService.showError(err, 'Failed to approve doctor.');
      }
    });
  }

  // Rejection modal states
  showRejectModal = false;
  selectedDoctorIdForRejection = '';
  rejectionReason = '';

  rejectDoctor(doctorUserId: string): void {
    this.openRejectModal(doctorUserId);
  }

  openRejectModal(doctorUserId: string): void {
    this.selectedDoctorIdForRejection = doctorUserId;
    this.rejectionReason = '';
    this.showRejectModal = true;
  }

  closeRejectModal(): void {
    this.showRejectModal = false;
    this.selectedDoctorIdForRejection = '';
    this.rejectionReason = '';
  }

  submitDoctorRejection(): void {
    if (!this.selectedDoctorIdForRejection || !this.rejectionReason.trim()) {
      this.toastService.showError('Please enter a rejection reason.');
      return;
    }

    this.adminService.rejectDoctor(this.selectedDoctorIdForRejection, this.rejectionReason).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message || 'Doctor application rejected.');
        this.closeRejectModal();
        this.loadDoctors();
      },
      error: (err) => {
        this.toastService.showError(err, 'Failed to reject doctor.');
      }
    });
  }
}
