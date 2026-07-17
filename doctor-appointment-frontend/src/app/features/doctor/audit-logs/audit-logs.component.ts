import { Component, OnInit, OnDestroy } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AppointmentService } from '../../../core/services/appointment.service';
import { AdminService } from '../../../core/services/admin.service';
import { AuthService } from '../../../core/services/auth.service';
import { NotificationService } from '../../../core/services/notification.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-audit-logs',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './audit-logs.component.html',
  styleUrls: ['./audit-logs.component.css']
})
export class AuditLogsComponent implements OnInit, OnDestroy {
  logs: any[] = [];
  clinics: any[] = [];
  selectedClinicId: string = '';
  
  currentPage: number = 1;
  pageSize: number = 10;
  totalLogs: number = 0;
  Math = Math;
  
  isLoading: boolean = false;
  userRole: string = '';
  private signalrSub?: Subscription;

  constructor(
    private appointmentService: AppointmentService,
    private adminService: AdminService,
    private authService: AuthService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.userRole = this.authService.getRole() || '';
    
    if (this.userRole === 'Doctor') {
      this.loadClinics();
    } else {
      this.loadLogs();
    }

    this.signalrSub = this.notificationService.refreshData$.subscribe({
      next: (area) => {
        if (area === 'AuditLogs') {
          this.loadLogs();
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
    this.adminService.getDoctorClinics().subscribe({
      next: (res: any) => {
        this.clinics = res || [];
        this.loadLogs();
      },
      error: (err: any) => {
        console.error('Failed to load clinics', err);
        this.loadLogs();
      }
    });
  }

  loadLogs(): void {
    this.isLoading = true;
    const clinicId = this.selectedClinicId ? this.selectedClinicId : undefined;
    
    this.appointmentService.getAuditLogs(this.currentPage, this.pageSize, clinicId).subscribe({
      next: (res: any) => {
        this.logs = res.items || [];
        this.totalLogs = res.totalCount || 0;
        this.isLoading = false;
      },
      error: (err: any) => {
        console.error('Failed to load audit logs', err);
        this.isLoading = false;
      }
    });
  }

  onClinicChange(): void {
    this.currentPage = 1;
    this.loadLogs();
  }

  changePage(page: number): void {
    if (page >= 1 && page <= this.totalPages) {
      this.currentPage = page;
      this.loadLogs();
    }
  }

  get totalPages(): number {
    return Math.ceil(this.totalLogs / this.pageSize) || 1;
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
}
