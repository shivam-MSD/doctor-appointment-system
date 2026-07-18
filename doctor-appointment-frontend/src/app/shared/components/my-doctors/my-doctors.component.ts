import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { AppointmentService } from '../../../core/services/appointment.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-my-doctors',
  templateUrl: './my-doctors.component.html',
  styleUrls: ['./my-doctors.component.css']
})
export class MyDoctorsComponent implements OnInit {
  @Output() bookDoctor = new EventEmitter<string>();

  isCareTeamLoading = true;
  consultedDoctors: any[] = [];
  selectedDoctorForInfo: any = null;
  selectedDoctorForHistory: any = null;

  constructor(private appointmentService: AppointmentService, private router: Router) {}

  ngOnInit(): void {
    this.loadCareTeam();
  }

  loadCareTeam(): void {
    this.appointmentService.getConsultedDoctors().subscribe({
      next: (docs: any[]) => {
        this.consultedDoctors = docs.map(doc => {
          if (doc.appointments) {
            doc.appointments = doc.appointments.filter((a: any) => a.status === 'Completed');
          }
          return doc;
        });
        this.isCareTeamLoading = false;
      },
      error: (err: any) => {
        console.error('Failed to load consulted doctors', err);
        this.isCareTeamLoading = false;
      }
    });
  }

  onBookAgain(doctorId: string): void {
    this.bookDoctor.emit(doctorId);
  }

  onBookClinic(doctorId: string, clinicId: string): void {
    this.closeDoctorInfo();
    this.router.navigate(['/patient/book-appointment'], { queryParams: { doctorId, clinicId } });
  }

  openDoctorInfo(doc: any): void {
    this.selectedDoctorForInfo = doc;
    document.body.style.overflow = 'hidden';
  }

  closeDoctorInfo(): void {
    this.selectedDoctorForInfo = null;
    document.body.style.overflow = '';
  }

  openAppointmentHistory(doc: any): void {
    this.selectedDoctorForHistory = doc;
    document.body.style.overflow = 'hidden';
  }

  closeAppointmentHistory(): void {
    this.selectedDoctorForHistory = null;
    document.body.style.overflow = '';
  }
}
