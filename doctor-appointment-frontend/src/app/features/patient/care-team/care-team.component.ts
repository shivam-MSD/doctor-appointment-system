import { Component, OnInit } from '@angular/core';
import { AppointmentService } from '../../../core/services/appointment.service';
import { AuthService } from '../../../core/services/auth.service';
import { PatientService } from '../../../core/services/patient.service';
import { Router } from '@angular/router';

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
  selectedDoctorForBooking: any = null;
  bookingClinics: any[] = [];
  bookingClinicsLoading = false;

  constructor(
    private appointmentService: AppointmentService,
    public authService: AuthService,
    private patientService: PatientService,
    private router: Router
  ) { }

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

  openBookAppointmentModal(doc: any): void {
    this.selectedDoctorForBooking = doc;
    this.bookingClinics = [];
    this.bookingClinicsLoading = true;

    this.patientService.getClinicsByDoctorId(doc.doctorId).subscribe({
      next: (res) => {
        this.bookingClinics = res;
        this.bookingClinicsLoading = false;

        // If doctor only has 1 clinic, navigate immediately
        if (res.length === 1) {
          const singleClinic = res[0];
          this.router.navigate(['/patient/book-appointment'], {
            queryParams: { doctorId: doc.doctorId, clinicId: singleClinic.clinicId }
          });
          this.closeBookAppointmentModal();
        } else if (res.length === 0) {
          // If no clinics registered yet, navigate to book screen with doctorId only
          this.router.navigate(['/patient/book-appointment'], {
            queryParams: { doctorId: doc.doctorId }
          });
          this.closeBookAppointmentModal();
        }
      },
      error: () => {
        this.bookingClinicsLoading = false;
        // Fallback to book page directly
        this.router.navigate(['/patient/book-appointment'], {
          queryParams: { doctorId: doc.doctorId }
        });
        this.closeBookAppointmentModal();
      }
    });
  }

  closeBookAppointmentModal(): void {
    this.selectedDoctorForBooking = null;
    this.bookingClinics = [];
    this.bookingClinicsLoading = false;
  }

  getLastClinicId(doc: any): string | null {
    if (!doc.appointments || doc.appointments.length === 0) {
      return null;
    }
    const sorted = [...doc.appointments].sort((a: any, b: any) => {
      return new Date(b.appointmentDate).getTime() - new Date(a.appointmentDate).getTime();
    });
    return sorted[0]?.clinicId || null;
  }

  isClinicCurrentlyOpen(clinic: any): boolean {
    if (!clinic || clinic.isAvailable === false) {
      return false;
    }

    if (!clinic.openDays) return false;
    const days = clinic.openDays.split(',').map((d: string) => d.trim().toLowerCase());
    const todayName = new Date().toLocaleDateString('en-US', { weekday: 'long' }).toLowerCase();
    if (!days.includes(todayName)) {
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
    if (!clinic || clinic.isAvailable === false) return false;
    if (!clinic.openDays || clinic.openDays.trim() === '') return false;

    const openDayNames = clinic.openDays.split(',').map((d: string) => d.trim().toLowerCase());
    const weekDays = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday'];

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    let rangeStart = new Date(today);
    if (clinic.bookingWindowStartDate) {
      const winStart = new Date(clinic.bookingWindowStartDate);
      winStart.setHours(0, 0, 0, 0);
      if (winStart > rangeStart) rangeStart = winStart;
    }

    let rangeEnd: Date | null = null;
    if (clinic.bookingWindowEndDate) {
      rangeEnd = new Date(clinic.bookingWindowEndDate);
      rangeEnd.setHours(23, 59, 59, 999);
      if (rangeEnd < today) return false;
    }

    if (rangeEnd && rangeStart > rangeEnd) return false;

    const scanLimit = rangeEnd
      ? Math.min(7, Math.ceil((rangeEnd.getTime() - rangeStart.getTime()) / 86400000) + 1)
      : 7;

    for (let i = 0; i < scanLimit; i++) {
      const d = new Date(rangeStart);
      d.setDate(d.getDate() + i);
      if (rangeEnd && d > rangeEnd) break;
      if (openDayNames.includes(weekDays[d.getDay()])) return true;
    }

    return false;
  }
}
