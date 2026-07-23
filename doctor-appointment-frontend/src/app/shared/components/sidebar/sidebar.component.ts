import { Component, OnInit, OnDestroy, Output, EventEmitter } from '@angular/core';
import { AuthService } from '../../../core/services/auth.service';
import { PatientService } from '../../../core/services/patient.service';
import { AdminService } from '../../../core/services/admin.service';
import { AppointmentService } from '../../../core/services/appointment.service';
import { NotificationService } from '../../../core/services/notification.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.css']
})
export class SidebarComponent implements OnInit, OnDestroy {
  @Output() menuItemClicked = new EventEmitter<void>();
  private profileCompletionLoaded = false;
  private profileCompletionValue = 100;
  private signalrSub?: Subscription;

  isCollapsed = false;

  // SuperAdmin pending counters
  pendingDoctorsCount = 0;
  pendingClinicsCount = 0;
  pendingAdminsCount = 0;

  // Patient pending counters
  patientPendingActionCount = 0;

  // Doctor pending counters
  pendingRequestsCount = 0;

  constructor(
    public authService: AuthService,
    private patientService: PatientService,
    private adminService: AdminService,
    private appointmentService: AppointmentService,
    private notificationService: NotificationService
  ) {}

  toggleSidebar(): void {
    this.isCollapsed = !this.isCollapsed;
    localStorage.setItem('sidebar_collapsed', this.isCollapsed.toString());
  }

  ngOnInit(): void {
    const savedState = localStorage.getItem('sidebar_collapsed');
    this.isCollapsed = savedState === 'true';

    // 1. Subscribe to dynamic authentication user changes to trigger initial counts
    this.authService.currentUser$.subscribe(user => {
      if (user) {
        this.loadCountsForRole(user.role);
      } else {
        this.resetCounts();
      }
    });

    // 2. Subscribe to SignalR refresh signals to update counters in real-time
    this.signalrSub = this.notificationService.refreshData$.subscribe({
      next: () => {
        const role = this.authService.getRole();
        if (role) {
          this.loadCountsForRole(role);
        }
      }
    });
  }

  private loadCountsForRole(role: string): void {
    if (role === 'Patient') {
      this.loadPatientCompletion();
      this.loadPatientCounts();
    } else if (role === 'SuperAdmin') {
      this.loadSuperAdminCounts();
    } else if (role === 'Doctor') {
      this.loadDoctorCounts();
    }
  }

  private resetCounts(): void {
    this.pendingDoctorsCount = 0;
    this.pendingClinicsCount = 0;
    this.pendingAdminsCount = 0;
    this.patientPendingActionCount = 0;
    this.pendingRequestsCount = 0;
    this.profileCompletionLoaded = false;
  }

  private loadSuperAdminCounts(): void {
    this.adminService.getPendingDoctors().subscribe({
      next: (res) => this.pendingDoctorsCount = res.length
    });
    this.adminService.getPendingClinics().subscribe({
      next: (res) => this.pendingClinicsCount = res.length
    });
    this.adminService.getPendingAdmins().subscribe({
      next: (res) => this.pendingAdminsCount = res.length
    });
  }

  private loadDoctorCounts(): void {
    this.appointmentService.getAdminDoctorDashboard({ status: 'Pending' }, 1, 1).subscribe({
      next: (res) => this.pendingRequestsCount = res.totalCount
    });
  }

  private loadPatientCounts(): void {
    this.appointmentService.getPatientDashboard('RescheduleProposed', false, 1, 1).subscribe({
      next: (res) => this.patientPendingActionCount = res.totalCount
    });
  }

  private loadPatientCompletion(): void {
    const profileId = sessionStorage.getItem('profileId');
    if (!profileId) return;

    this.patientService.getPatientProfile(profileId).subscribe({
      next: (data: any) => {
        const stats = this.calculatePatientStats(data);
        this.profileCompletionValue = stats.percentage;
        this.profileCompletionLoaded = true;
        sessionStorage.setItem('profileCompletion', stats.percentage.toString());
      }
    });
  }

  private calculatePatientStats(data: any): { percentage: number } {
    let completed = 0;
    if (data.firstName && data.firstName.trim()) completed += 15;
    if (data.lastName && data.lastName.trim()) completed += 15;
    if (data.mobileNo && data.mobileNo.trim()) completed += 15;
    if (data.gender) completed += 15;
    if (data.dob && data.dob !== '0001-01-01') completed += 15;
    if (data.bloodGroup) completed += 15;
    if (data.emergencyContactName && data.emergencyContactName.trim() &&
        data.emergencyContactNumber && data.emergencyContactNumber.trim()) {
      completed += 10;
    }
    return { percentage: Math.min(completed, 100) };
  }

  getCompletionPercentage(): number {
    return this.profileCompletionValue;
  }

  isProfileIncomplete(): boolean {
    const role = this.authService.getRole();
    if (role !== 'Patient') return false;
    return this.profileCompletionLoaded && this.profileCompletionValue < 100;
  }

  ngOnDestroy(): void {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
  }
}
