import { Component, OnInit, Output, EventEmitter } from '@angular/core';
import { AppointmentService } from '../../../core/services/appointment.service';
import { PatientService } from '../../../core/services/patient.service';
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
  selectedDoctorForBooking: any = null;
  bookingClinics: any[] = [];
  bookingClinicsLoading = false;
  expandedNotes: { [appId: string]: boolean } = {};

  toggleNoteExpansion(appId: string): void {
    this.expandedNotes[appId] = !this.expandedNotes[appId];
  }

  constructor(
    private appointmentService: AppointmentService,
    private patientService: PatientService,
    private router: Router
  ) {}

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
    this.closeBookAppointmentModal();
    this.router.navigate(['/patient/book-appointment'], { queryParams: { doctorId, clinicId } });
    setTimeout(() => {
      const contentArea = document.querySelector('.content-area');
      if (contentArea) {
        contentArea.scrollTo({ top: 0, behavior: 'smooth' });
      } else {
        window.scrollTo({ top: 0, behavior: 'smooth' });
      }
    }, 150);
  }

  openDoctorInfo(doc: any): void {
    this.selectedDoctorForInfo = { ...doc };
    document.body.style.overflow = 'hidden';

    // If city or state is missing/N/A, auto-populate from clinics API
    if (!this.selectedDoctorForInfo.city || this.selectedDoctorForInfo.city === 'N/A') {
      this.patientService.getClinicsByDoctorId(doc.doctorId).subscribe({
        next: (clinics: any[]) => {
          if (clinics && clinics.length > 0) {
            const firstClinic = clinics[0];
            if (this.selectedDoctorForInfo) {
              this.selectedDoctorForInfo.city = firstClinic.city || 'Vadodara';
              this.selectedDoctorForInfo.state = firstClinic.state || 'Gujarat';
            }
          }
        },
        error: () => {}
      });
    }
  }

  closeDoctorInfo(): void {
    this.selectedDoctorForInfo = null;
    document.body.style.overflow = '';
  }

  openAppointmentHistory(doc: any): void {
    this.selectedDoctorForHistory = { ...doc, appointments: [], isLoadingHistory: true };
    document.body.style.overflow = 'hidden';

    this.appointmentService.getDoctorHistory(doc.doctorId).subscribe({
      next: (history) => {
        if (this.selectedDoctorForHistory && this.selectedDoctorForHistory.doctorId === doc.doctorId) {
          this.selectedDoctorForHistory.appointments = history;
          this.selectedDoctorForHistory.isLoadingHistory = false;
        }
      },
      error: (err) => {
        console.error('Failed to load doctor history', err);
        if (this.selectedDoctorForHistory) {
          this.selectedDoctorForHistory.isLoadingHistory = false;
        }
      }
    });
  }

  closeAppointmentHistory(): void {
    this.selectedDoctorForHistory = null;
    document.body.style.overflow = '';
  }

  openBookAppointmentModal(doc: any): void {
    this.selectedDoctorForBooking = doc;
    this.bookingClinics = [];
    this.bookingClinicsLoading = true;
    document.body.style.overflow = 'hidden';

    // Lazy-load clinics via API call for this specific doctor
    this.patientService.getClinicsByDoctorId(doc.doctorId).subscribe({
      next: (res: any[]) => {
        this.bookingClinicsLoading = false;
        const clinicsList = res || [];

        if (clinicsList.length === 1) {
          // If doctor has exactly 1 clinic branch, navigate directly to booking
          const singleClinic = clinicsList[0];
          this.closeBookAppointmentModal();
          this.router.navigate(['/patient/book-appointment'], {
            queryParams: { doctorId: doc.doctorId, clinicId: singleClinic.clinicId }
          });
        } else if (clinicsList.length === 0) {
          // If no clinics registered yet, navigate to booking with doctorId only
          this.closeBookAppointmentModal();
          this.router.navigate(['/patient/book-appointment'], {
            queryParams: { doctorId: doc.doctorId }
          });
        } else {
          // Multiple clinics -> display selection list in modal / mobile bottom-sheet
          this.bookingClinics = clinicsList;
        }
      },
      error: (err) => {
        console.error('Failed to load clinics for doctor', err);
        this.bookingClinicsLoading = false;
        this.bookingClinics = [];
      }
    });
  }

  closeBookAppointmentModal(): void {
    this.selectedDoctorForBooking = null;
    this.bookingClinics = [];
    this.bookingClinicsLoading = false;
    document.body.style.overflow = '';
  }

  isClinicCurrentlyOpen(clinic: any): boolean {
    if (!clinic || clinic.isAvailable === false || clinic.isDoctorAvailable === false) {
      return false;
    }

    if (!clinic.openDays) return false;
    const days = clinic.openDays.split(',').map((d: string) => d.trim().toLowerCase());
    const todayLong = new Date().toLocaleDateString('en-US', { weekday: 'long' }).toLowerCase();
    const todayShort = new Date().toLocaleDateString('en-US', { weekday: 'short' }).toLowerCase();
    const isOpenToday = days.some((d: string) => d === todayLong || d === todayShort || d.startsWith(todayShort));
    if (!isOpenToday) {
      return false;
    }

    if (!clinic.startTime || !clinic.endTime) return false;
    const starts = clinic.startTime.split(',').map((t: string) => t.trim());
    const ends = clinic.endTime.split(',').map((t: string) => t.trim());

    const parseTimeToMinutes = (timeStr: string): number => {
      if (!timeStr) return 0;
      const ampmMatch = timeStr.match(/(\d+):(\d+)\s*(AM|PM)/i);
      if (ampmMatch) {
        let hours = parseInt(ampmMatch[1], 10);
        const minutes = parseInt(ampmMatch[2], 10);
        const ampm = ampmMatch[3].toUpperCase();
        if (ampm === 'PM' && hours < 12) hours += 12;
        if (ampm === 'AM' && hours === 12) hours = 0;
        return hours * 60 + minutes;
      }
      const parts = timeStr.split(':');
      if (parts.length >= 2) {
        const hours = parseInt(parts[0], 10);
        const minutes = parseInt(parts[1], 10);
        return hours * 60 + minutes;
      }
      return 0;
    };

    const now = new Date();
    const currentMinutes = now.getHours() * 60 + now.getMinutes();

    for (let i = 0; i < starts.length; i++) {
      if (!starts[i] || !ends[i]) continue;
      const startMin = parseTimeToMinutes(starts[i]);
      const endMin = parseTimeToMinutes(ends[i]);
      
      if (startMin <= endMin) {
        if (currentMinutes >= startMin && currentMinutes <= endMin) {
          return true;
        }
      } else {
        if (currentMinutes >= startMin || currentMinutes <= endMin) {
          return true;
        }
      }
    }
    return false;
  }

  isClinicBookable(clinic: any): boolean {
    if (!clinic) return false;
    if (clinic.isAvailable === false) return false;
    return true;
  }
}
