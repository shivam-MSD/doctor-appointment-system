import { Component, OnInit, OnDestroy } from '@angular/core';
import { AdminService } from '../../../core/services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';
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
  private signalrSub?: Subscription;

  // View Details modal states
  showDetailsModal = false;
  selectedDoctorForDetails: any = null;
  isDetailsVerified = false;

  constructor(
    private adminService: AdminService,
    private notificationService: NotificationService
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
    this.adminService.getAllDoctors(this.searchQuery, this.statusFilter).subscribe({
      next: (res) => {
        this.doctors = res;
      },
      error: () => {
        this.errorMessage = 'Failed to load doctors list.';
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
        this.successMessage = res.message || 'Doctor approved successfully!';
        this.loadDoctors();
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail || 'Failed to approve doctor.';
      }
    });
  }

  rejectDoctor(doctorUserId: string): void {
    this.adminService.verifyDoctor(doctorUserId, 'Rejected').subscribe({
      next: (res) => {
        this.successMessage = res.message || 'Doctor application rejected.';
        this.loadDoctors();
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail || 'Failed to reject doctor.';
      }
    });
  }
}
