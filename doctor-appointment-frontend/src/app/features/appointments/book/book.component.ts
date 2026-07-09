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
  startTime = '';
  endTime = '';
  consultationType = 'InPerson';
  reason = '';

  // Timing Slot Definitions
  todayDate = '';
  minBookingDate = '';
  maxBookingDate = '';
  generatedSlots: any[] = [];
  selectedSlot: any = null;
  weekDaysList = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
  dateValidationError = '';
  isClinicLocked = false;
  selectedDoctor: any = null;
  selectedClinic: any = null;
  currentMonth = new Date();
  calendarDays: any[] = [];

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
  ) {}

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

          if (qDoctorId) {
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
                      if (qClinicId && clinicList.some(c => c.clinicId === qClinicId)) {
                        this.clinicId = qClinicId;
                        this.selectedClinic = clinicList.find(c => c.clinicId === qClinicId);
                        this.onClinicChange();
                      } else if (clinicList.length > 0) {
                        this.clinicId = clinicList[0].clinicId;
                        this.selectedClinic = clinicList[0];
                        this.onClinicChange();
                      }
                      
                      // Ensure default Mode is InPerson, and if the clinic doesn't support Video we reset it
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
    this.generatedSlots = [];
    this.selectedSlot = null;
    this.startTime = '';
    this.endTime = '';
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

      const isAvailable = !isPast && !exceedsMin && !exceedsMax && !isClosedDay;
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
    this.generatedSlots = [];
    this.selectedSlot = null;
    this.startTime = '';
    this.endTime = '';
    this.dateValidationError = '';

    if (!this.appointmentDate || !this.clinicId || !this.doctorId) {
      return;
    }

    const selectedClinic = this.getSelectedClinic();
    if (!selectedClinic) return;

    // 1. Determine day of the week (adjusting for timezone if needed, simple local parse is fine)
    const dateObj = new Date(this.appointmentDate);
    const dayName = this.weekDaysList[dateObj.getDay()];

    // 2. Validate clinic is open on this day of week
    const openDaysArray = selectedClinic.openDays ? selectedClinic.openDays.split(',').map((d: string) => d.trim()) : [];
    if (!openDaysArray.includes(dayName)) {
      this.dateValidationError = `This clinic branch is closed on ${dayName}. Open days: ${selectedClinic.openDays}.`;
      return;
    }

    // 3. Generate candidate slots
    const startStr = selectedClinic.startTime || '';
    const endStr = selectedClinic.endTime || '';

    let starts = startStr.includes(',') ? startStr.split(',') : [startStr];
    let ends = endStr.includes(',') ? endStr.split(',') : [endStr];

    const slots: any[] = [];
    const count = Math.min(starts.length, ends.length);

    for (let i = 0; i < count; i++) {
      const s = starts[i]?.trim();
      const e = ends[i]?.trim();
      if (!s || !e) continue;

      let currentMin = this.timeToMinutes(s);
      const endMin = this.timeToMinutes(e);

      // Generate 30 minutes slots
      while (currentMin + 30 <= endMin) {
        const nextMin = currentMin + 30;
        const slotStart = this.minutesToTime(currentMin);
        const slotEnd = this.minutesToTime(nextMin);
        slots.push({
          startTime: slotStart,
          endTime: slotEnd,
          isBooked: false,
          label: `${this.formatTime12(slotStart)} - ${this.formatTime12(slotEnd)}`
        });
        currentMin = nextMin;
      }
    }

    this.generatedSlots = slots;

    // 4. Query booked slots from backend
    this.appointmentService.getBookedSlots(this.doctorId, this.clinicId, this.appointmentDate, this.patientId).subscribe({
      next: (bookedList: any[]) => {
        for (const slot of this.generatedSlots) {
          const isOverlapping = bookedList.some(booked => {
            return slot.startTime < booked.endTime && slot.endTime > booked.startTime;
          });
          if (isOverlapping) {
            slot.isBooked = true;
          }
        }
      },
      error: () => {
        this.toastService.showError('Failed to fetch already booked slots.');
      }
    });
  }

  selectSlot(slot: any): void {
    if (slot.isBooked) return;
    this.selectedSlot = slot;
    this.startTime = slot.startTime;
    this.endTime = slot.endTime;
  }

  private timeToMinutes(timeStr: string): number {
    const parts = timeStr.split(':');
    const hrs = parseInt(parts[0] || '0', 10);
    const mins = parseInt(parts[1] || '0', 10);
    return hrs * 60 + mins;
  }

  private minutesToTime(totalMins: number): string {
    const hrs = Math.floor(totalMins / 60);
    const mins = totalMins % 60;
    return `${String(hrs).padStart(2, '0')}:${String(mins).padStart(2, '0')}`;
  }

  private formatTime12(timeStr: string): string {
    const parts = timeStr.split(':');
    let hrs = parseInt(parts[0] || '0', 10);
    const mins = parts[1] || '00';
    const ampm = hrs >= 12 ? 'PM' : 'AM';
    hrs = hrs % 12;
    if (hrs === 0) hrs = 12;
    return `${hrs}:${mins} ${ampm}`;
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

    const selectedClinic = this.getSelectedClinic();

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
