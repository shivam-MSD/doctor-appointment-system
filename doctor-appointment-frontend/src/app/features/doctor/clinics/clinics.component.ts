import { Component, OnInit, OnDestroy } from '@angular/core';
import { AdminService } from '../../../core/services/admin.service';
import { ToastService } from '../../../core/services/toast.service';
import { NotificationService } from '../../../core/services/notification.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-clinics',
  templateUrl: './clinics.component.html',
  styleUrls: ['./clinics.component.css']
})
export class ClinicsComponent implements OnInit, OnDestroy {
  doctorClinics: any[] = [];
  errorMessage = '';
  isClinicsLoading = true;
  successMessage = '';
  showClinicModal = false;
  showAdminModal = false;
  selectedClinicIdForAdmin = '';
  selectedClinicNameForAdmin = '';
  private signalrSub?: Subscription;

  // Weekday definitions
  weekDays = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
  selectedDaysRegister: string[] = [];
  selectedDaysEdit: string[] = [];
  selectedDaysTimings: string[] = [];
  selectedRescheduleDaysTimings: string[] = [];

  // Edit clinic states
  showEditClinicModal = false;
  showTimingsModal = false;
  showAdminDetailsModal = false;
  showClinicDetailsModal = false;
  selectedClinicDetails: any = null;

  // Selected Admin Details
  selectedAdminName = '';
  selectedAdminEmail = '';
  selectedAdminMobileNo = '';
  selectedAdminClinicName = '';
  selectedAdminIsVerified = false;

  // Split shift variables
  isSplitShift = false;
  startTime1 = '';
  endTime1 = '';
  startTime2 = '';
  endTime2 = '';
  timingsErrorMessage = '';

  // Booking Window Calendar state
  bookingCalMonth = new Date();
  bookingCalDays: any[] = [];
  bookingPickStart = '';
  bookingPickEnd = '';

  selectedClinicIdForTimings = '';
  selectedClinicNameForTimings = '';
  clinicTimingsForm = {
    openDays: '',
    startTime: '',
    endTime: '',
    isAvailable: true,
    unavailabilityReason: '',
    isDoctorAvailable: true,
    doctorUnavailabilityReason: '',
    bookingWindowStartDate: '',
    bookingWindowEndDate: '',
    supportInPerson: true,
    supportVideo: false,
    maxAppointmentsPerDay: null as number | null
  };

  selectedClinicIdForEdit = '';
  clinicEditForm = {
    clinicName: '',
    clinicType: 'Clinic',
    country: 'India',
    state: '',
    city: '',
    area: '',
    pincode: '',
    addressline1: '',
    addressline2: '',
    openDays: '',
    startTime: '',
    endTime: '',
    isAvailable: true,
    unavailabilityReason: ''
  };

  clinicOnlyForm = {
    clinicName: '',
    clinicType: 'Clinic',
    country: 'India',
    state: '',
    city: '',
    area: '',
    pincode: '',
    addressline1: '',
    addressline2: '',
    openDays: '',
    startTime: '',
    endTime: '',
    isAvailable: true,
    unavailabilityReason: ''
  };

  adminForm = {
    clinicId: '',
    adminEmail: '',
    adminPassword: '',
    adminFirstName: '',
    adminLastName: '',
    adminMobileNo: ''
  };

  constructor(
    private adminService: AdminService,
    private toastService: ToastService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadDoctorClinics();

    // Auto-reload doctor clinics table in real-time when refresh signals are received
    this.signalrSub = this.notificationService.refreshData$.subscribe({
      next: (area) => {
        if (area === 'Clinics') {
          this.loadDoctorClinics();
        }
      }
    });
  }

  getSortedDays(openDaysStr: string): string[] {
    if (!openDaysStr) return [];
    const days = openDaysStr.split(',')
      .map(d => d.trim())
      .map(d => d.toLowerCase().startsWith('[reschedule:') ? d.replace(/\[reschedule:(.*?)\]/i, '$1 (Reschedule)') : d);
    return days.sort((a, b) => this.weekDays.indexOf(a.replace(' (Reschedule)', '')) - this.weekDays.indexOf(b.replace(' (Reschedule)', '')));
  }

  formatClinicTimings(startTime: string, endTime: string): string {
    if (!startTime || !endTime) return '';
    const starts = startTime.split(',').map(t => t.trim());
    const ends = endTime.split(',').map(t => t.trim());
    const shifts: string[] = [];
    for (let i = 0; i < starts.length; i++) {
      if (starts[i] && ends[i]) {
        shifts.push(`${starts[i]} - ${ends[i]}`);
      }
    }
    return shifts.join(' & ');
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

  ngOnDestroy(): void {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
  }

  loadDoctorClinics(): void {
    this.isClinicsLoading = true;
    this.adminService.getDoctorClinics().subscribe({
      next: (res) => {
        this.doctorClinics = res;
        this.isClinicsLoading = false;
      },
      error: () => {
        this.errorMessage = 'Failed to load clinics.';
        this.isClinicsLoading = false;
      }
    });
  }

  openClinicModal(): void {
    this.showClinicModal = true;
    this.errorMessage = '';
    this.successMessage = '';
  }

  closeClinicModal(): void {
    this.showClinicModal = false;
    this.selectedDaysRegister = [];
    this.clinicOnlyForm = {
      clinicName: '',
      clinicType: 'Clinic',
      country: 'India',
      state: '',
      city: '',
      area: '',
      pincode: '',
      addressline1: '',
      addressline2: '',
      openDays: '',
      startTime: '',
      endTime: '',
      isAvailable: true,
      unavailabilityReason: ''
    };
  }

  toggleDayRegister(day: string): void {
    const index = this.selectedDaysRegister.indexOf(day);
    if (index > -1) {
      this.selectedDaysRegister.splice(index, 1);
    } else {
      this.selectedDaysRegister.push(day);
    }
    this.clinicOnlyForm.openDays = this.selectedDaysRegister.join(',');
  }

  isDaySelectedRegister(day: string): boolean {
    return this.selectedDaysRegister.includes(day);
  }

  toggleDayEdit(day: string): void {
    const index = this.selectedDaysEdit.indexOf(day);
    if (index > -1) {
      this.selectedDaysEdit.splice(index, 1);
    } else {
      this.selectedDaysEdit.push(day);
    }
    this.clinicEditForm.openDays = this.selectedDaysEdit.join(',');
  }

  isDaySelectedEdit(day: string): boolean {
    return this.selectedDaysEdit.includes(day);
  }

  openAdminModal(clinicId: string, clinicName: string): void {
    this.selectedClinicIdForAdmin = clinicId;
    this.selectedClinicNameForAdmin = clinicName;
    this.adminForm.clinicId = clinicId;
    this.showAdminModal = true;
    this.errorMessage = '';
    this.successMessage = '';
  }

  closeAdminModal(): void {
    this.showAdminModal = false;
    this.adminForm = {
      clinicId: '',
      adminEmail: '',
      adminPassword: '',
      adminFirstName: '',
      adminLastName: '',
      adminMobileNo: ''
    };
  }

  validateClinicForm(form: any): boolean {
    const requiredFields = ['clinicName', 'clinicType', 'country', 'state', 'city', 'area', 'pincode', 'addressline1'];
    for (const field of requiredFields) {
      const val = form[field];
      if (val === undefined || val === null || (typeof val === 'string' && val.trim() === '')) {
        const fieldNameFormatted = field.replace(/([A-Z])/g, ' $1').replace(/^./, str => str.toUpperCase());
        this.toastService.showError(`${fieldNameFormatted} is required and cannot be empty or blank.`);
        return false;
      }
    }
    return true;
  }

  submitClinicOnly(): void {
    if (!this.validateClinicForm(this.clinicOnlyForm)) {
      return;
    }
    this.adminService.registerClinicOnly(this.clinicOnlyForm).subscribe({
      next: (res) => {
        this.successMessage = res.message || 'Clinic registered successfully! Pending Super Admin verification.';
        this.toastService.showSuccess(this.successMessage);
        this.loadDoctorClinics();
        setTimeout(() => this.closeClinicModal(), 2000);
      },
      error: (err) => {
        const errorDetail = err?.error?.detail || 'Failed to register clinic.';
        this.toastService.showError(errorDetail);
      }
    });
  }

  submitAdmin(): void {
    this.adminService.registerClinicAdmin(this.adminForm).subscribe({
      next: (res) => {
        this.successMessage = res.message || 'Clinic Admin registered successfully! Pending Super Admin verification.';
        this.toastService.showSuccess(this.successMessage);
        this.loadDoctorClinics();
        setTimeout(() => this.closeAdminModal(), 2000);
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail || 'Failed to register clinic admin.';
        this.toastService.showError(this.errorMessage);
      }
    });
  }

  // Edit clinic methods
  // Edit clinic methods
  openEditClinicModal(clinic: any): void {
    this.selectedClinicIdForEdit = clinic.clinicId;
    this.selectedDaysEdit = clinic.openDays ? clinic.openDays.split(',').map((d: string) => d.trim()) : [];
    this.clinicEditForm = {
      clinicName: clinic.clinicName,
      clinicType: clinic.clinicType,
      country: 'India',
      state: clinic.state,
      city: clinic.city,
      area: clinic.area || '',
      pincode: clinic.pincode || '',
      addressline1: clinic.addressline1 || '',
      addressline2: clinic.addressline2 || '',
      openDays: clinic.openDays || '',
      startTime: clinic.startTime || '',
      endTime: clinic.endTime || '',
      isAvailable: clinic.isAvailable !== false,
      unavailabilityReason: clinic.unavailabilityReason || ''
    };
    this.showEditClinicModal = true;
  }

  closeEditClinicModal(): void {
    this.showEditClinicModal = false;
    this.selectedClinicIdForEdit = '';
    this.selectedDaysEdit = [];
  }

  submitClinicEdit(): void {
    if (!this.selectedClinicIdForEdit) return;

    if (!this.validateClinicForm(this.clinicEditForm)) {
      return;
    }

    this.adminService.updateClinic(this.selectedClinicIdForEdit, this.clinicEditForm).subscribe({
      next: () => {
        this.toastService.showSuccess('Clinic details updated. Awaiting Super Admin verification.');
        this.closeEditClinicModal();
        this.loadDoctorClinics();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to update clinic details.');
      }
    });
  }

  // Timings Edit modal methods
  openTimingsModal(clinic: any): void {
    this.selectedClinicIdForTimings = clinic.clinicId;
    this.selectedClinicNameForTimings = clinic.clinicName;
    
    // Parse normal days and reschedule-only days
    const allDays = clinic.openDays ? clinic.openDays.split(',').map((d: string) => d.trim()) : [];
    this.selectedDaysTimings = allDays.filter((d: string) => !d.toLowerCase().startsWith('[reschedule:'));
    this.selectedRescheduleDaysTimings = allDays
      .filter((d: string) => d.toLowerCase().startsWith('[reschedule:'))
      .map((d: string) => {
        const match = d.match(/\[reschedule:(.*?)\]/i);
        return match ? match[1] : d;
      });
    
    const startTimeStr = clinic.startTime || '';
    const endTimeStr = clinic.endTime || '';

    if (startTimeStr.includes(',')) {
      this.isSplitShift = true;
      this.startTime1 = startTimeStr.split(',')[0]?.trim() || '';
      this.startTime2 = startTimeStr.split(',')[1]?.trim() || '';
      this.endTime1 = endTimeStr.split(',')[0]?.trim() || '';
      this.endTime2 = endTimeStr.split(',')[1]?.trim() || '';
    } else {
      this.isSplitShift = false;
      this.startTime1 = startTimeStr;
      this.endTime1 = endTimeStr;
      this.startTime2 = '';
      this.endTime2 = '';
    }

    const modes = clinic.supportedModes || '';
    this.clinicTimingsForm = {
      openDays: clinic.openDays || '',
      startTime: startTimeStr,
      endTime: endTimeStr,
      isAvailable: clinic.isAvailable !== false,
      unavailabilityReason: clinic.unavailabilityReason || '',
      isDoctorAvailable: clinic.isDoctorAvailable !== false,
      doctorUnavailabilityReason: clinic.doctorUnavailabilityReason || '',
      bookingWindowStartDate: clinic.bookingWindowStartDate ? clinic.bookingWindowStartDate.substring(0, 10) : '',
      bookingWindowEndDate: clinic.bookingWindowEndDate ? clinic.bookingWindowEndDate.substring(0, 10) : '',
      supportInPerson: !modes || modes.includes('InPerson'),
      supportVideo: modes.includes('VideoConsultation'),
      maxAppointmentsPerDay: clinic.maxAppointmentsPerDay ?? null
    };
    
    this.bookingPickStart = this.clinicTimingsForm.bookingWindowStartDate;
    this.bookingPickEnd = this.clinicTimingsForm.bookingWindowEndDate;
    this.bookingCalMonth = this.bookingPickStart ? new Date(this.bookingPickStart) : new Date();
    this.generateBookingCalendar();

    this.showTimingsModal = true;
  }

  closeTimingsModal(): void {
    this.showTimingsModal = false;
    this.selectedClinicIdForTimings = '';
    this.selectedClinicNameForTimings = '';
    this.selectedDaysTimings = [];
    this.isSplitShift = false;
    this.startTime1 = '';
    this.endTime1 = '';
    this.startTime2 = '';
    this.endTime2 = '';
    this.timingsErrorMessage = '';
    this.bookingPickStart = '';
    this.bookingPickEnd = '';
    this.clinicTimingsForm.bookingWindowStartDate = '';
    this.clinicTimingsForm.bookingWindowEndDate = '';
  }

  toggleDayTimings(day: string): void {
    const index = this.selectedDaysTimings.indexOf(day);
    if (index > -1) {
      this.selectedDaysTimings.splice(index, 1);
    } else {
      this.selectedDaysTimings.push(day);
    }
    // Sort array elements by standard weekday index sequence
    this.selectedDaysTimings.sort((a, b) => this.weekDays.indexOf(a) - this.weekDays.indexOf(b));
    
    // Ensure day is not in both arrays
    if (this.selectedRescheduleDaysTimings.includes(day)) {
      this.selectedRescheduleDaysTimings = this.selectedRescheduleDaysTimings.filter(d => d !== day);
    }
    
    this.clinicTimingsForm.openDays = this.buildOpenDaysString();
    
    this.bookingPickStart = '';
    this.bookingPickEnd = '';
    this.clinicTimingsForm.bookingWindowStartDate = '';
    this.clinicTimingsForm.bookingWindowEndDate = '';
    this.generateBookingCalendar();
  }

  isDaySelectedTimings(day: string): boolean {
    return this.selectedDaysTimings.includes(day);
  }

  isDaySelectedReschedule(day: string): boolean {
    return this.selectedRescheduleDaysTimings.includes(day);
  }

  toggleDayReschedule(day: string): void {
    if (this.isDaySelectedReschedule(day)) {
      this.selectedRescheduleDaysTimings = this.selectedRescheduleDaysTimings.filter(d => d !== day);
    } else {
      this.selectedRescheduleDaysTimings.push(day);
    }
    // Sort array elements by standard weekday index sequence
    this.selectedRescheduleDaysTimings.sort((a, b) => this.weekDays.indexOf(a) - this.weekDays.indexOf(b));
    
    // Ensure day is not in both arrays
    if (this.selectedDaysTimings.includes(day)) {
      this.selectedDaysTimings = this.selectedDaysTimings.filter(d => d !== day);
    }
    
    this.clinicTimingsForm.openDays = this.buildOpenDaysString();
    
    this.bookingPickStart = '';
    this.bookingPickEnd = '';
    this.clinicTimingsForm.bookingWindowStartDate = '';
    this.clinicTimingsForm.bookingWindowEndDate = '';
    this.generateBookingCalendar();
  }

  private buildOpenDaysString(): string {
    const normalDays = [...this.selectedDaysTimings];
    const rescheduleDays = this.selectedRescheduleDaysTimings.map(d => `[Reschedule:${d}]`);
    return [...normalDays, ...rescheduleDays].join(', ');
  }



  submitClinicTimings(): void {
    if (!this.selectedClinicIdForTimings) return;

    if (this.isSplitShift) {
      if (!this.startTime1 || !this.endTime1 || !this.startTime2 || !this.endTime2) {
        this.toastService.showError('Please configure both timing shifts completely.');
        return;
      }
      if (this.startTime1 >= this.endTime1) {
        this.toastService.showError('Shift 1 opening time must be before closing time.');
        return;
      }
      if (this.startTime2 >= this.endTime2) {
        this.toastService.showError('Shift 2 opening time must be before closing time.');
        return;
      }
      if (this.endTime1 > this.startTime2) {
        this.toastService.showError('Shift 1 closing time cannot be after Shift 2 opening time.');
        return;
      }
      this.clinicTimingsForm.startTime = `${this.startTime1},${this.startTime2}`;
      this.clinicTimingsForm.endTime = `${this.endTime1},${this.endTime2}`;
    } else {
      if (!this.startTime1 || !this.endTime1) {
        this.toastService.showError('Please configure opening and closing hours.');
        return;
      }
      if (this.startTime1 >= this.endTime1) {
        this.toastService.showError('Opening time must be before closing time.');
        return;
      }
      this.clinicTimingsForm.startTime = this.startTime1;
      this.clinicTimingsForm.endTime = this.endTime1;
    }

    if (this.clinicTimingsForm.isAvailable) {
      if (!this.clinicTimingsForm.openDays || !this.clinicTimingsForm.startTime || !this.clinicTimingsForm.endTime) {
        this.toastService.showError('Active/Open clinics must have a timing schedule (open days, start time, and end time) configured.');
        return;
      }
    }

    // Fetch full existing clinic to preserve name and address when submitting timings
    const existing = this.doctorClinics.find(c => c.clinicId === this.selectedClinicIdForTimings);
    if (!existing) return;

    const modesList: string[] = [];
    if (this.clinicTimingsForm.supportInPerson) modesList.push('InPerson');
    if (this.clinicTimingsForm.supportVideo) modesList.push('VideoConsultation');
    const supportedModesStr = modesList.join(',');

    const payload = {
      clinicName: existing.clinicName,
      clinicType: existing.clinicType,
      country: existing.country || 'India',
      state: existing.state,
      city: existing.city,
      area: existing.area || '',
      pincode: existing.pincode || '',
      addressline1: existing.addressline1 || '',
      addressline2: existing.addressline2 || '',
      openDays: this.clinicTimingsForm.openDays,
      startTime: this.clinicTimingsForm.startTime,
      endTime: this.clinicTimingsForm.endTime,
      isAvailable: this.clinicTimingsForm.isAvailable,
      unavailabilityReason: this.clinicTimingsForm.unavailabilityReason,
      isDoctorAvailable: this.clinicTimingsForm.isDoctorAvailable,
      doctorUnavailabilityReason: this.clinicTimingsForm.doctorUnavailabilityReason,
      bookingWindowStartDate: this.clinicTimingsForm.bookingWindowStartDate ? new Date(this.clinicTimingsForm.bookingWindowStartDate).toISOString() : null,
      bookingWindowEndDate: this.clinicTimingsForm.bookingWindowEndDate ? new Date(this.clinicTimingsForm.bookingWindowEndDate).toISOString() : null,
      supportedModes: supportedModesStr,
      maxAppointmentsPerDay: this.clinicTimingsForm.maxAppointmentsPerDay || null
    };

    this.adminService.updateClinic(this.selectedClinicIdForTimings, payload).subscribe({
      next: () => {
        this.toastService.showSuccess('Clinic timings and schedule updated successfully.');
        this.closeTimingsModal();
        this.loadDoctorClinics();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to update clinic timings.');
      }
    });
  }

  validateTimingsChange(): void {
    this.timingsErrorMessage = '';

    if (this.isSplitShift) {
      if (this.startTime1 && this.endTime1 && this.startTime1 >= this.endTime1) {
        this.timingsErrorMessage = 'Session 1 opening time must be before closing time.';
        return;
      }
      if (this.startTime2 && this.endTime2 && this.startTime2 >= this.endTime2) {
        this.timingsErrorMessage = 'Session 2 opening time must be before closing time.';
        return;
      }
      if (this.endTime1 && this.startTime2 && this.endTime1 > this.startTime2) {
        this.timingsErrorMessage = 'Session 1 closing time cannot be after Session 2 opening time.';
        return;
      }
    } else {
      if (this.startTime1 && this.endTime1 && this.startTime1 >= this.endTime1) {
        this.timingsErrorMessage = 'Opening time must be before closing time.';
        return;
      }
    }
  }

  // Admin details popup modal handlers
  openAdminDetailModal(clinic: any): void {
    this.selectedAdminName = clinic.adminName || 'Pending Assignment';
    this.selectedAdminEmail = clinic.adminEmail || 'N/A';
    this.selectedAdminMobileNo = clinic.adminMobileNo || 'N/A';
    this.selectedAdminClinicName = clinic.clinicName;
    this.selectedAdminIsVerified = clinic.adminIsVerified !== false;
    this.showAdminDetailsModal = true;
  }

  closeAdminDetailModal(): void {
    this.showAdminDetailsModal = false;
    this.selectedAdminName = '';
    this.selectedAdminEmail = '';
    this.selectedAdminMobileNo = '';
    this.selectedAdminClinicName = '';
    this.selectedAdminIsVerified = false;
  }

  openClinicDetailsModal(clinic: any): void {
    this.selectedClinicDetails = clinic;
    this.showClinicDetailsModal = true;
  }

  closeClinicDetailsModal(): void {
    this.showClinicDetailsModal = false;
    this.selectedClinicDetails = null;
  }

  // Booking Window Calendar Actions
  generateBookingCalendar(): void {
    this.bookingCalDays = this.generateBookingCalendarDays(
      this.bookingCalMonth,
      this.selectedDaysTimings,
      this.bookingPickStart,
      this.bookingPickEnd
    );
  }

  bookingCalPrev(): void {
    const m = this.bookingCalMonth.getMonth();
    this.bookingCalMonth = new Date(this.bookingCalMonth.getFullYear(), m - 1, 1);
    this.generateBookingCalendar();
  }

  bookingCalNext(): void {
    const m = this.bookingCalMonth.getMonth();
    this.bookingCalMonth = new Date(this.bookingCalMonth.getFullYear(), m + 1, 1);
    this.generateBookingCalendar();
  }

  onBookingDayClick(day: any): void {
    if (!day.isOpenDay) return;
    const clicked = day.dateString;

    if (!this.bookingPickStart || (this.bookingPickStart && this.bookingPickEnd)) {
      this.bookingPickStart = clicked;
      this.bookingPickEnd = '';
    } else {
      if (clicked < this.bookingPickStart) {
        this.bookingPickEnd = this.bookingPickStart;
        this.bookingPickStart = clicked;
      } else {
        this.bookingPickEnd = clicked;
      }
    }
    this.clinicTimingsForm.bookingWindowStartDate = this.bookingPickStart;
    this.clinicTimingsForm.bookingWindowEndDate = this.bookingPickEnd;
    this.generateBookingCalendar();
  }

  clearBookingWindow(): void {
    this.bookingPickStart = '';
    this.bookingPickEnd = '';
    this.clinicTimingsForm.bookingWindowStartDate = '';
    this.clinicTimingsForm.bookingWindowEndDate = '';
    this.generateBookingCalendar();
  }

  getBookingCalMonthName(): string {
    return this.bookingCalMonth.toLocaleString('default', { month: 'long', year: 'numeric' });
  }

  // Generate calendar days for a given month and selected range
  generateBookingCalendarDays(
    monthDate: Date,
    selectedDays: string[],
    pickStart: string,
    pickEnd: string
  ): any[] {
    const year = monthDate.getFullYear();
    const month = monthDate.getMonth();
    const firstDay = new Date(year, month, 1);
    const lastDay = new Date(year, month + 1, 0);

    const startingDayOfWeek = firstDay.getDay(); // 0 = Sunday
    const daysInMonth = lastDay.getDate();

    const days: any[] = [];
    const todayStr = new Date().toISOString().substring(0, 10);

    for (let i = 0; i < startingDayOfWeek; i++) {
      days.push({ dayNumber: null });
    }

    const dayNameMap = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

    for (let i = 1; i <= daysInMonth; i++) {
      const d = new Date(year, month, i);
      const dateString = d.toISOString().substring(0, 10);
      const dayName = dayNameMap[d.getDay()];

      const isOpenDay = selectedDays.includes(dayName) && dateString >= todayStr;
      const isStart = dateString === pickStart;
      const isEnd = dateString === pickEnd;
      const inRange = pickStart && pickEnd ? dateString > pickStart && dateString < pickEnd : false;
      const isToday = dateString === todayStr;

      days.push({
        dayNumber: i,
        dateString: dateString,
        isOpenDay: isOpenDay,
        isStart: isStart,
        isEnd: isEnd,
        inRange: inRange,
        isToday: isToday
      });
    }

    return days;
  }
}
