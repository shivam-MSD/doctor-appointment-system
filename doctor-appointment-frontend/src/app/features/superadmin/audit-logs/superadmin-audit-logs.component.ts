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
    document.body.style.overflow = 'hidden';
  }

  closeDetails(): void {
    this.showDetailsModal = false;
    this.selectedLog = null;
    document.body.style.overflow = '';
  }

  parseJson(jsonString: string): any {
    try {
      return jsonString ? JSON.parse(jsonString) : null;
    } catch {
      return null;
    }
  }
}
