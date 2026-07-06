import { Component, OnInit } from '@angular/core';
import { AdminService } from '../../../core/services/admin.service';

@Component({
  selector: 'app-super-admin-doctors',
  templateUrl: './super-admin-doctors.component.html',
  styleUrls: ['./super-admin-doctors.component.css']
})
export class SuperAdminDoctorsComponent implements OnInit {
  doctors: any[] = [];
  searchQuery = '';
  statusFilter = ''; // Empty for all, Verified, Pending
  errorMessage = '';
  successMessage = '';

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.loadDoctors();
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
