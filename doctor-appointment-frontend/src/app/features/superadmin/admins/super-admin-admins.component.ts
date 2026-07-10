import { Component, OnInit, OnDestroy } from '@angular/core';
import { AdminService } from '../../../core/services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-super-admin-admins',
  templateUrl: './super-admin-admins.component.html',
  styleUrls: ['./super-admin-admins.component.css']
})
export class SuperAdminAdminsComponent implements OnInit, OnDestroy {
  admins: any[] = [];
  searchQuery = '';
  verifiedFilter: boolean | undefined = undefined;
  errorMessage = '';
  successMessage = '';
  private signalrSub?: Subscription;

  // View Details modal states
  showDetailsModal = false;
  selectedAdminForDetails: any = null;
  isDetailsVerified = false;

  constructor(
    private adminService: AdminService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadAdmins();

    // Auto-reload the clinic admin applications roster in real-time when refresh signals are received
    this.signalrSub = this.notificationService.refreshData$.subscribe({
      next: (area) => {
        if (area === 'Admins') {
          this.loadAdmins();
        }
      }
    });
  }

  ngOnDestroy(): void {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
  }

  loadAdmins(): void {
    this.adminService.getAllAdmins(this.searchQuery, this.verifiedFilter).subscribe({
      next: (res) => {
        this.admins = res;
      },
      error: () => {
        this.errorMessage = 'Failed to load clinic administrators list.';
      }
    });
  }

  openDetailsModal(admin: any): void {
    this.selectedAdminForDetails = admin;
    this.isDetailsVerified = false;
    this.showDetailsModal = true;
  }

  closeDetailsModal(): void {
    this.showDetailsModal = false;
    this.selectedAdminForDetails = null;
    this.isDetailsVerified = false;
  }

  verifyAdmin(adminId: string): void {
    this.adminService.verifyAdmin(adminId).subscribe({
      next: (res) => {
        this.successMessage = res.message || 'Clinic Admin approved successfully!';
        this.loadAdmins();
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail || 'Failed to approve admin.';
      }
    });
  }

  rejectAdmin(adminId: string): void {
    this.adminService.rejectAdmin(adminId).subscribe({
      next: (res) => {
        this.successMessage = res.message || 'Clinic Admin rejected successfully.';
        this.loadAdmins();
        setTimeout(() => this.successMessage = '', 3000);
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail || 'Failed to reject admin.';
      }
    });
  }
}
