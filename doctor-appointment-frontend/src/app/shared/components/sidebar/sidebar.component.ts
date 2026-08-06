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
    this.patientService.getPatientSelfProfile().subscribe({
      next: (data: any) => {
        const stats = this.calculatePatientStats(data);
        this.profileCompletionValue = stats.percentage;
        this.profileCompletionLoaded = true;
        sessionStorage.setItem('profileCompletion', stats.percentage.toString());
      },
      error: () => {
        const cached = sessionStorage.getItem('profileCompletion');
        if (cached) {
          this.profileCompletionValue = parseInt(cached, 10) || 100;
          this.profileCompletionLoaded = true;
        }
      }
    });
  }

  private calculatePatientStats(data: any): { percentage: number } {
    let completed = 0;
    const fName = data?.firstName || '';
    const lName = data?.lastName || '';
    const mob = data?.mobileNo || data?.mobile || '';
    const gen = data?.gender || '';
    const birthDate = data?.dob ? data.dob.split('T')[0] : '';
    const bGroup = data?.bloodGroup || '';
    const emName = data?.emergencyContactName || '';
    const emNum = data?.emergencyContactNumber || '';

    if (fName && fName.trim()) completed += 15;
    if (lName && lName.trim()) completed += 15;
    if (mob && mob.trim() && mob !== 'Not Added') completed += 15;
    if (gen) completed += 15;
    if (birthDate && birthDate !== '0001-01-01' && birthDate !== '0001-01-01T00:00:00') completed += 15;
    if (bGroup) completed += 15;
    if (emName && emName.trim() && emNum && emNum.trim()) completed += 10;
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

  getProfileLink(): string {
    const role = this.authService.getRole();
    if (role === 'Patient') return '/patient/profile';
    if (role === 'Doctor') return '/doctor/profile';
    if (role === 'Admin') return '/admin/profile';
    return '/patient/profile';
  }

  ngOnDestroy(): void {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
  }
}
