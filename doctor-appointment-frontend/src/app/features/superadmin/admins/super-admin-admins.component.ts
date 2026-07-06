import { Component, OnInit } from '@angular/core';
import { AdminService } from '../../../core/services/admin.service';

@Component({
  selector: 'app-super-admin-admins',
  templateUrl: './super-admin-admins.component.html',
  styleUrls: ['./super-admin-admins.component.css']
})
export class SuperAdminAdminsComponent implements OnInit {
  admins: any[] = [];
  searchQuery = '';
  verifiedFilter: boolean | undefined = undefined;
  errorMessage = '';
  successMessage = '';

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.loadAdmins();
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
}
