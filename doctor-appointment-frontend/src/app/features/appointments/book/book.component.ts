import { Component, OnInit } from '@angular/core';
import { Router, ActivatedRoute } from '@angular/router';
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
  consultationType = 'InPerson';
  reason = '';

  // Calendar state
  todayDate = '';
  minBookingDate = '';
  maxBookingDate = '';
  weekDaysList = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
  dateValidationError = '';
  isClinicLocked = false;
  selectedDoctor: any = null;
  selectedClinic: any = null;
  currentMonth = new Date();
  calendarDays: any[] = [];

  // Day availability info (for selected date)
  dayAvailability: any = null;
  isLoadingAvailability = false;

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
    private route: ActivatedRoute,
    private toastService: ToastService
  ) { }

  ngOnInit(): void {
    const today = new Date();
    const yyyy = today.getFullYear();
    const mm = String(today.getMonth() + 1).padStart(2, '0');
    const dd = String(today.getDate()).padStart(2, '0');
    this.todayDate = `${yyyy}-${mm}-${dd}`;

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

        // 3. Process router query parameters after patient list is loaded
        this.route.queryParams.subscribe(params => {
          const qDoctorId = params['doctorId'];
          const qClinicId = params['clinicId'];
          this.isClinicLocked = !!qClinicId;

          if (qDoctorId && qClinicId) {
            this.appointmentService.getBookingDetails(qDoctorId, qClinicId).subscribe({
              next: (details) => {
                this.selectedDoctor = details.doctor;
                this.doctors = [details.doctor];
                this.doctorId = qDoctorId;

                this.selectedClinic = details.clinic;
                this.clinics = [details.clinic];
                this.clinicId = qClinicId;

                this.onClinicChange();
                this.consultationType = 'InPerson';
              },
              error: (err) => {
                this.toastService.showError(err, 'Failed to load booking details.');
                this.router.navigate(['/patient/dashboard']);
              }
            });
          } else if (qDoctorId) {
            this.appointmentService.getAvailableDoctors().subscribe({
              next: (allDoctors) => {
                const foundDoctor = allDoctors.find(d => d.doctorId === qDoctorId);
                if (foundDoctor) {
                  this.doctors = [foundDoctor];
                  this.doctorId = qDoctorId;
                  this.selectedDoctor = foundDoctor;

                  this.appointmentService.getClinicsForDoctor(qDoctorId).subscribe({
                    next: (clinicList) => {
                      this.clinics = clinicList;
                      if (clinicList.length > 0) {
                        this.clinicId = clinicList[0].clinicId;
                        this.selectedClinic = clinicList[0];
                        this.onClinicChange();
                      }
                      this.consultationType = 'InPerson';
                    }
                  });
                }
              }
            });
          }
        });
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
          this.onClinicChange();
        }
      }
    });
  }

  onClinicChange(): void {
    this.appointmentDate = '';
    this.dayAvailability = null;
    this.dateValidationError = '';

    const selectedClinic = this.getSelectedClinic();
    this.selectedClinic = selectedClinic;
    if (selectedClinic && selectedClinic.bookingWindowStartDate) {
      this.minBookingDate = selectedClinic.bookingWindowStartDate.substring(0, 10);
    } else {
      this.minBookingDate = '';
    }
    if (selectedClinic && selectedClinic.bookingWindowEndDate) {
      this.maxBookingDate = selectedClinic.bookingWindowEndDate.substring(0, 10);
    } else {
      this.maxBookingDate = '';
    }

    this.currentMonth = new Date();
    this.generateCalendar();
  }

  getSelectedClinic(): any {
    return this.clinics.find(c => c.clinicId === this.clinicId);
  }

  generateCalendar(): void {
    const year = this.currentMonth.getFullYear();
    const month = this.currentMonth.getMonth();

    const firstDay = new Date(year, month, 1);
    const startDayOfWeek = firstDay.getDay();

    const totalDays = new Date(year, month + 1, 0).getDate();

    const days: any[] = [];

    // Add padding days for alignment
    for (let i = 0; i < startDayOfWeek; i++) {
      days.push({ dayNumber: null, dateString: '', isAvailable: false });
    }

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const minLimitDate = this.minBookingDate ? new Date(this.minBookingDate) : null;
    if (minLimitDate) {
      minLimitDate.setHours(0, 0, 0, 0);
    }

    const maxLimitDate = this.maxBookingDate ? new Date(this.maxBookingDate) : null;
    if (maxLimitDate) {
      maxLimitDate.setHours(23, 59, 59, 999);
    }

    const openDaysArray = this.selectedClinic?.openDays
      ? this.selectedClinic.openDays.split(',').map((d: string) => d.trim())
      : [];

    for (let day = 1; day <= totalDays; day++) {
      const dateObj = new Date(year, month, day);
      dateObj.setHours(0, 0, 0, 0);

      const yyyy = dateObj.getFullYear();
      const mm = String(dateObj.getMonth() + 1).padStart(2, '0');
      const dd = String(dateObj.getDate()).padStart(2, '0');
      const dateString = `${yyyy}-${mm}-${dd}`;

      const dayName = this.weekDaysList[dateObj.getDay()];

      const isPast = dateObj < today;
      const exceedsMin = minLimitDate ? dateObj < minLimitDate : false;
      const exceedsMax = maxLimitDate ? dateObj > maxLimitDate : false;
      const isClosedDay = !openDaysArray.includes(dayName);

      // If clinic is manually closed, all days are unavailable
      const isClinicManuallyOpen = this.selectedClinic?.isAvailable !== false;

      const isAvailable = isClinicManuallyOpen && !isPast && !exceedsMin && !exceedsMax && !isClosedDay;
      const isToday = dateObj.getTime() === today.getTime();

      days.push({
        dayNumber: day,
        dateString,
        isAvailable,
        isToday,
        isPast,
        exceedsMin,
        exceedsMax,
        isClosedDay
      });
    }

    this.calendarDays = days;
  }

  prevMonth(): void {
    const m = this.currentMonth.getMonth();
    this.currentMonth = new Date(this.currentMonth.getFullYear(), m - 1, 1);
    this.generateCalendar();
  }

  nextMonth(): void {
    const m = this.currentMonth.getMonth();
    this.currentMonth = new Date(this.currentMonth.getFullYear(), m + 1, 1);
    this.generateCalendar();
  }

  selectCalendarDate(day: any): void {
    if (!day.isAvailable) return;
    this.appointmentDate = day.dateString;
    this.onDateChange();
  }

  getMonthName(): string {
    return this.currentMonth.toLocaleString('default', { month: 'long', year: 'numeric' });
  }

  onDateChange(): void {
    this.dayAvailability = null;
    this.dateValidationError = '';

    if (!this.appointmentDate || !this.clinicId) {
      return;
    }

    const selectedClinic = this.getSelectedClinic();
    if (!selectedClinic) return;

    // Block if clinic is manually closed
    if (selectedClinic.isAvailable === false) {
      this.dateValidationError = 'This clinic branch is temporarily closed and is not accepting appointments.';
      return;
    }

    // Validate clinic is open on this day of week
    const dateObj = new Date(this.appointmentDate + 'T00:00:00');
    const dayName = this.weekDaysList[dateObj.getDay()];
    const openDaysArray = selectedClinic?.openDays ? selectedClinic.openDays.split(',').map((d: string) => d.trim()) : [];
    if (openDaysArray.length > 0 && !openDaysArray.includes(dayName)) {
      this.dateValidationError = `This clinic is closed on ${dayName}. Open days: ${selectedClinic?.openDays}.`;
      return;
    }

    // Fetch day availability from backend
    this.isLoadingAvailability = true;
    this.appointmentService.getDayAvailability(this.clinicId, this.appointmentDate).subscribe({
      next: (avail) => {
        this.dayAvailability = avail;
        this.isLoadingAvailability = false;
        if (avail.isFull) {
          this.dateValidationError = `This date is fully booked (${avail.bookedCount}/${avail.maxCapacity} appointments). Please choose another date.`;
        }
      },
      error: () => {
        this.isLoadingAvailability = false;
      }
    });
  }

  onSubmit(): void {
    if (!this.patientId || !this.doctorId || !this.appointmentDate) {
      this.errorMessage = 'Please complete all required fields and select an appointment date.';
      this.toastService.showError(this.errorMessage);
      return;
    }

    if (this.clinics.length > 0 && !this.clinicId) {
      this.errorMessage = 'Please select a clinic/location for your appointment.';
      this.toastService.showError(this.errorMessage);
      return;
    }

    const selectedClinic = this.getSelectedClinic();

    // Guard: clinic manually closed
    if (selectedClinic && selectedClinic.isAvailable === false) {
      this.errorMessage = 'This clinic branch is temporarily closed and cannot accept appointments.';
      this.toastService.showError(this.errorMessage);
      return;
    }

    // Guard: day is fully booked
    if (this.dayAvailability?.isFull) {
      this.errorMessage = 'This date is fully booked. Please choose another date.';
      this.toastService.showError(this.errorMessage);
      return;
    }

    const payload = {
      patientId: this.patientId,
      doctorId: this.doctorId,
      clinicId: this.clinicId ? this.clinicId : null,
      appointmentDate: this.appointmentDate,
      consultationType: this.consultationType,
      reason: this.reason
    };

    this.appointmentService.bookAppointment(payload).subscribe({
      next: (result) => {
        this.errorMessage = '';
        const queueNum = result?.queueNumber ? ` You are #${result.queueNumber} in queue.` : '';
        this.successMessage = `Appointment booked successfully!${queueNum} Redirecting to dashboard...`;
        this.toastService.showSuccess(this.successMessage);
        setTimeout(() => {
          this.router.navigate(['/patient/dashboard']);
        }, 2000);
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail || 'Failed to book appointment. The date may be fully booked.';
        this.toastService.showError(this.errorMessage);
      }
    });
  }
}
