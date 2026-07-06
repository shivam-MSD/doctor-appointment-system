import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';
import { AppointmentService } from '../../../core/services/appointment.service';
import { FamilyService } from '../../../core/services/family.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-book',
  templateUrl: './book.component.html',
  styleUrls: ['./book.component.css']
})
export class BookComponent implements OnInit {
  // Booking Fields
  patientId = '';
  doctorId = '';
  clinicId = '';
  appointmentDate = '';
  startTime = '';
  endTime = '';
  consultationType = 'InPerson';
  reason = '';

  // Mandatory Filter Fields
  state = '';
  city = '';
  specializationId = '';
  
  // Custom Filter Field
  nameSearch = '';

  // Lists
  patients: any[] = [];
  specializations: any[] = [];
  doctors: any[] = [];
  clinics: any[] = [];

  // Messages
  errorMessage = '';
  successMessage = '';

  constructor(
    private appointmentService: AppointmentService,
    private familyService: FamilyService,
    private router: Router,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    // 1. Load specializations dropdown list
    this.appointmentService.getSpecializations().subscribe({
      next: (data) => {
        this.specializations = data;
      },
      error: () => {
        this.errorMessage = 'Failed to load specializations list.';
      }
    });

    // 2. Fetch linked patient profiles
    this.familyService.getFamilyMembers().subscribe({
      next: (data) => {
        this.patients = data;
        if (data.length > 0) {
          this.patientId = data[0].patientId;
        }
      },
      error: () => {
        this.errorMessage = 'Failed to load patient profiles.';
      }
    });
  }

  // Trigger search whenever any filter (mandatory or custom name) is modified
  onFilterChange(): void {
    if (!this.state && !this.city && !this.specializationId && !this.nameSearch) {
      this.doctors = [];
      this.doctorId = '';
      this.clinics = [];
      this.clinicId = '';
      return;
    }

    this.appointmentService.searchDoctors(
      this.state || undefined, 
      this.city || undefined, 
      this.specializationId || undefined, 
      this.nameSearch || undefined
    ).subscribe({
      next: (data) => {
        this.doctors = data;
        if (data.length > 0) {
          this.doctorId = data[0].doctorId;
          this.onDoctorChange();
        } else {
          this.doctorId = '';
          this.clinics = [];
          this.clinicId = '';
        }
      },
      error: () => {
        this.errorMessage = 'Failed to query doctors list matching your search filters.';
      }
    });
  }

  onDoctorChange(): void {
    this.clinicId = '';
    this.clinics = [];
    if (!this.doctorId) {
      return;
    }
    this.appointmentService.getClinicsForDoctor(this.doctorId).subscribe({
      next: (data) => {
        this.clinics = data;
        if (data.length > 0) {
          this.clinicId = data[0].clinicId;
        }
      }
    });
  }

  onSubmit(): void {
    if (!this.patientId || !this.doctorId || !this.appointmentDate || !this.startTime || !this.endTime || !this.reason) {
      this.errorMessage = 'Please complete all required fields (make sure to select a Doctor).';
      this.toastService.showError(this.errorMessage);
      return;
    }

    if (this.clinics.length > 0 && !this.clinicId) {
      this.errorMessage = 'Please select a clinic/location for your appointment.';
      this.toastService.showError(this.errorMessage);
      return;
    }

    // Combine date and time strings into complete Date objects
    const startDateTime = new Date(`${this.appointmentDate}T${this.startTime}`);
    const endDateTime = new Date(`${this.appointmentDate}T${this.endTime}`);

    if (startDateTime >= endDateTime) {
      this.errorMessage = 'Start time must be strictly before end time.';
      this.toastService.showError(this.errorMessage);
      return;
    }

    const payload = {
      patientId: this.patientId,
      doctorId: this.doctorId,
      clinicId: this.clinicId ? this.clinicId : null,
      appointmentDate: this.appointmentDate,
      startTime: startDateTime.toISOString(),
      endTime: endDateTime.toISOString(),
      consultationType: this.consultationType,
      reason: this.reason
    };

    this.appointmentService.bookAppointment(payload).subscribe({
      next: () => {
        this.errorMessage = '';
        this.successMessage = 'Appointment booked successfully! Redirecting to dashboard...';
        this.toastService.showSuccess(this.successMessage);
        setTimeout(() => {
          this.router.navigate(['/patient/dashboard']);
        }, 2000);
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail || 'An overlap conflict occurred. Please select another slot.';
        this.toastService.showError(this.errorMessage);
      }
    });
  }
}
