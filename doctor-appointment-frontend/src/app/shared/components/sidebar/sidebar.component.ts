import { Component, OnInit, OnDestroy } from '@angular/core';
import { AuthService } from '../../../core/services/auth.service';
import { PatientService } from '../../../core/services/patient.service';
import { AdminService } from '../../../core/services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.css']
})
export class SidebarComponent implements OnInit, OnDestroy {
  private profileCompletionLoaded = false;
  private profileCompletionValue = 100;
  private signalrSub?: Subscription;

  // SuperAdmin pending counters
  pendingDoctorsCount = 0;
  pendingClinicsCount = 0;
  pendingAdminsCount = 0;

  constructor(
    public authService: AuthService,
    private patientService: PatientService,
    private adminService: AdminService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    const role = this.authService.getRole();
    if (role === 'Patient') {
      this.loadPatientCompletion();
    } else if (role === 'SuperAdmin') {
      this.loadSuperAdminCounts();
      
      // Auto-refresh counts in real-time
      this.signalrSub = this.notificationService.refreshData$.subscribe({
        next: () => {
          this.loadSuperAdminCounts();
        }
      });
    }
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
