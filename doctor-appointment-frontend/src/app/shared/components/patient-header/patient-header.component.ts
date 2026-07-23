import { Component, OnInit, Input } from '@angular/core';
import { forkJoin } from 'rxjs';
import { AuthService } from '../../../core/services/auth.service';
import { AppointmentService } from '../../../core/services/appointment.service';

@Component({
  selector: 'app-patient-header',
  templateUrl: './patient-header.component.html',
  styleUrls: ['./patient-header.component.css']
})
export class PatientHeaderComponent implements OnInit {
  @Input() showWelcomeBanner = true;

  firstName = '';
  isPatientStatsLoading = true;
  patientTotalCompleted = 0;
  patientTotalUpcoming = 0;
  patientTotalPending = 0;
  patientTotalRescheduled = 0;
  patientTotalToday = 0;

  constructor(
    private authService: AuthService,
    private appointmentService: AppointmentService
  ) {}

  ngOnInit(): void {
    const userStr = localStorage.getItem('user');
    if (userStr) {
      const user = JSON.parse(userStr);
      this.firstName = user.firstName || 'Patient';
    }

    if (this.authService.getRole() === 'Patient') {
      this.loadPatientStats();
    }
  }

  loadPatientStats(): void {
    // Generate local YYYY-MM-DD string to avoid UTC shifting
    const today = new Date();
    const y = today.getFullYear();
    const m = String(today.getMonth() + 1).padStart(2, '0');
    const d = String(today.getDate()).padStart(2, '0');
    const todayStr = `${y}-${m}-${d}`;
    
    this.isPatientStatsLoading = true;
    forkJoin({
      upcomingRes: this.appointmentService.getPatientDashboard('', false, 1, 1000),
      historyRes: this.appointmentService.getPatientDashboard('', true, 1, 1000)
    }).subscribe({
      next: ({ upcomingRes, historyRes }) => {
        const allApps = [...upcomingRes.items, ...historyRes.items];
        
        this.patientTotalCompleted = allApps.filter(a => a.status === 'Completed').length;
        this.patientTotalPending = allApps.filter(a => a.status === 'Pending').length;
        this.patientTotalRescheduled = allApps.filter(a => a.status === 'RescheduleProposed').length;
        
        // Today: confirmed count for today's date
        this.patientTotalToday = allApps.filter(a => {
          if (!a.appointmentDate) return false;
          return a.appointmentDate.startsWith(todayStr) && a.status === 'Confirmed';
        }).length;

        // Upcoming: active/pending appointments specifically AFTER today
        this.patientTotalUpcoming = allApps.filter(a => {
          if (!a.appointmentDate) return false;
          const isToday = a.appointmentDate.startsWith(todayStr);
          const isActive = a.status === 'Confirmed' || a.status === 'Pending' || a.status === 'RescheduleProposed';
          const appDate = new Date(a.appointmentDate);
          const todayDate = new Date();
          todayDate.setHours(0, 0, 0, 0);
          
          return !isToday && isActive && appDate > todayDate;
        }).length;
        
        this.isPatientStatsLoading = false;
      },
      error: (err) => {
        console.error('Failed to load patient stats', err);
        this.isPatientStatsLoading = false;
      }
    });
  }
}
