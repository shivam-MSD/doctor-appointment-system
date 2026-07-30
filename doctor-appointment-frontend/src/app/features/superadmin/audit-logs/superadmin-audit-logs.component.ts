import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpClient } from '@angular/common/http';
import { ToastService } from '../../../core/services/toast.service';


@Component({
  selector: 'app-superadmin-audit-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './superadmin-audit-logs.component.html',
  styleUrls: ['./superadmin-audit-logs.component.css']
})
export class SuperadminAuditLogsComponent implements OnInit {
  auditLogs: any[] = [];
  isLoading = true;
  errorMessage = '';

  // Filters
  entityType = '';
  action = '';
  startDate = '';
  endDate = '';

  // Pagination
  currentPage = 1;
  pageSize = 10;
  totalCount = 0;
  Math = Math;

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.pageSize);
  }

  // Detail Modal
  showDetailsModal = false;
  selectedLog: any = null;
  viewMode: 'friendly' | 'json' = 'friendly';

  constructor(private http: HttpClient, private toastService: ToastService) {}

  ngOnInit(): void {
    this.loadLogs();
  }

  loadLogs(): void {
    this.isLoading = true;
    let params: any = {
      page: this.currentPage,
      size: this.pageSize
    };

    if (this.entityType) params.entityType = this.entityType;
    if (this.action) params.action = this.action;
    if (this.startDate) params.startDate = this.startDate;
    if (this.endDate) params.endDate = this.endDate;

    this.http.get<any>('/api/admin/system-audit-logs', { params }).subscribe({
      next: (res) => {
        this.auditLogs = res.items || [];
        this.totalCount = res.totalCount || 0;
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Failed to load system audit logs.';
        this.toastService.showError('Failed to load system audit logs.');
        this.isLoading = false;
      }
    });
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadLogs();
  }

  applyFilters(): void {
    this.currentPage = 1;
    this.loadLogs();
  }

  resetFilters(): void {
    this.entityType = '';
    this.action = '';
    this.startDate = '';
    this.endDate = '';
    this.currentPage = 1;
    this.loadLogs();
  }

  openDetails(log: any): void {
    this.selectedLog = log;
    this.showDetailsModal = true;
    const contentArea = document.querySelector('.content-area') as HTMLElement;
    if (contentArea) {
      contentArea.style.overflow = 'hidden';
    }
  }

  closeDetails(): void {
    this.showDetailsModal = false;
    this.selectedLog = null;
    const contentArea = document.querySelector('.content-area') as HTMLElement;
    if (contentArea) {
      contentArea.style.overflow = '';
    }
  }

  parseJson(jsonString: string): any {
    try {
      return jsonString ? JSON.parse(jsonString) : null;
    } catch {
      return null;
    }
  }

  getPageNumbers(): (number | string)[] {
    const total = this.totalPages;
    const current = this.currentPage;
    const delta = 2; 
    const range: number[] = [];
    const rangeWithDots: (number | string)[] = [];
    let l: number | undefined;

    for (let i = 1; i <= total; i++) {
      if (i == 1 || i == total || (i >= current - delta && i < current + delta + 1)) {
        range.push(i);
      }
    }

    for (let i of range) {
      if (l) {
        if (i - l === 2) {
          rangeWithDots.push(l + 1);
        } else if (i - l !== 1) {
          rangeWithDots.push('...');
        }
      }
      rangeWithDots.push(i);
      l = i;
    }

    return rangeWithDots;
  }

  formatVal(key: string, val: any): string {
    if (val === null || val === undefined || val === '') {
      return '(empty)';
    }
    if (typeof val === 'boolean') {
      return val ? 'Yes' : 'No';
    }

    const keyLower = key.toLowerCase();

    // Map EVerificationStatus
    if (keyLower === 'verificationstatus') {
      const num = Number(val);
      if (num === 0) return 'Pending';
      if (num === 1) return 'Verified';
      if (num === 2) return 'Rejected';
      if (num === 3) return 'UpdatedPending';
    }

    // Map EAppointmentStatus / Status
    if (keyLower === 'status' || keyLower === 'appointmentstatus') {
      const num = Number(val);
      if (num === 0) return 'Pending';
      if (num === 1) return 'Confirmed';
      if (num === 2) return 'Cancelled';
      if (num === 3) return 'Completed';
      if (num === 4) return 'Rejected';
      if (num === 5) return 'Expired';
      if (num === 6) return 'RescheduleProposed';
      if (num === 7) return 'FollowUpProposed';
    }

    // Map ERole / Role
    if (keyLower === 'role') {
      const num = Number(val);
      if (num === 0) return 'SuperAdmin';
      if (num === 1) return 'Admin';
      if (num === 2) return 'Doctor';
      if (num === 3) return 'Patient';
    }

    // Map EGender / Gender
    if (keyLower === 'gender') {
      const num = Number(val);
      if (num === 0) return 'Male';
      if (num === 1) return 'Female';
      if (num === 2) return 'Other';
    }

    return String(val);
  }

  getDiffList(oldJson: string, newJson: string): any[] {
    const oldObj = this.parseJson(oldJson) || {};
    const newObj = this.parseJson(newJson) || {};
    const allKeys = Array.from(new Set([...Object.keys(oldObj), ...Object.keys(newObj)]));
    const diffs: any[] = [];

    // Filter out standard non-user-facing metadata keys
    const excludeKeys = ['createddate', 'updateddate', 'createdat', 'updatedat', 'id'];

    allKeys.forEach(key => {
      if (excludeKeys.includes(key.toLowerCase())) return;

      const oldVal = oldObj[key];
      const newVal = newObj[key];

      if (JSON.stringify(oldVal) !== JSON.stringify(newVal)) {
        diffs.push({
          key: key,
          label: key.replace(/([A-Z])/g, ' $1').trim(),
          oldVal: this.formatVal(key, oldVal),
          newVal: this.formatVal(key, newVal)
        });
      }
    });

    return diffs;
  }
}
