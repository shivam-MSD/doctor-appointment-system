import { Component, OnInit } from '@angular/core';
import { AppointmentService } from '../../../core/services/appointment.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-care-team',
  templateUrl: './care-team.component.html',
  styleUrls: ['./care-team.component.css']
})
export class CareTeamComponent implements OnInit {
  consultedDoctors: any[] = [];
  isLoading = true;

  selectedDoctorForInfo: any = null;
  selectedDoctorForHistory: any = null;

  constructor(
    private appointmentService: AppointmentService,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    if (this.authService.getRole() === 'Patient') {
      this.loadCareTeam();
    }
  }

  loadCareTeam(): void {
    this.appointmentService.getConsultedDoctors().subscribe({
      next: (docs: any[]) => {
        // Only keep 'Completed' appointments for the Care Team statistics and history
        this.consultedDoctors = docs.map(doc => {
          if (doc.appointments) {
            doc.appointments = doc.appointments.filter((a: any) => a.status === 'Completed');
          }
          return doc;
        });
        this.isLoading = false;
      },
      error: (err: any) => {
        console.error('Failed to load consulted doctors', err);
        this.isLoading = false;
      }
    });
  }

  openDoctorInfo(doc: any): void {
    this.selectedDoctorForInfo = doc;
  }

  closeDoctorInfo(): void {
    this.selectedDoctorForInfo = null;
  }

  openAppointmentHistory(doc: any): void {
    this.selectedDoctorForHistory = doc;
  }

  closeAppointmentHistory(): void {
    this.selectedDoctorForHistory = null;
  }
}
