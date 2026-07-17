import { Component, OnInit, OnDestroy } from '@angular/core';
import { AuthService } from '../../core/services/auth.service';
import { AppointmentService } from '../../core/services/appointment.service';
import { AdminService } from '../../core/services/admin.service';
import { PatientService } from '../../core/services/patient.service';
import { ToastService } from '../../core/services/toast.service';
import { NotificationService } from '../../core/services/notification.service';
import { Appointment } from '../../core/models/appointment.model';
import { Subscription } from 'rxjs';
import { ActivatedRoute } from '@angular/router';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  role = '';
  appointments: Appointment[] = [];
  statusFilter = '';
  dateFilter = '';
  showSuccessModal = false;
  successMessage = '';
  consultationFilter = '';
  firstName = '';
  errorMessage = '';
  historyMode = false;

  // Reschedule Propose Modal State
  showRescheduleModal = false;
  selectedRescheduleAppId = '';
  rescheduleDate = '';
  rescheduleTime = '';
  rescheduleReason = '';

  patientPage = 1;
  patientSize = 10;
  private signalrSub?: Subscription;

  // Doctor completeness state
  isDoctorAddressIncomplete = false;

  // Doctor Daily Queue States
  doctorPage = 1;
  doctorSize = 10;
  commentInputs: { [key: string]: string } = {};
  noteInputs: { [key: string]: string } = {};
  reportInputs: { [key: string]: string } = {};
  expandedNoteRows: { [key: string]: boolean } = {};

  // Warning Confirmation Modals
  showCompleteConfirm = false;
  selectedAppIdForComplete = '';
  showNoShowConfirm = false;
  selectedAppIdForNoShow = '';
  showCancelAppointmentConfirm = false;
  selectedAppIdForCancel = '';

  // Reject Modal State
  showRejectConfirm = false;
  selectedAppIdForReject = '';
  rejectReasonInput = '';

  // Assign Time Modal States
  showAssignTimeModal = false;
  selectedAppIdForAssignTime = '';
  assignedTimeInput = '';

  // Patient Details Modal States
  showPatientDetailsModal = false;
  selectedPatientDetails: any = null;
  isDetailsLoading = false;

  // Clinic Details Modal States
  showClinicDetailsModal = false;
  selectedClinicDetails: any = null;

  // Doctor Dashboard Patient History modal states
  showHistoryModal = false;
  selectedPatientName = '';
  patientHistory: Appointment[] = [];
  isHistoryLoading = false;

  // Main Loading Flags
  isDashboardLoading = true;
  isClinicsLoading = true;
  isSuperAdminLoading = true;

  // Patient Dashboard own appointment notes modal states
  showPatientHistoryModal = false;
  selectedAppForHistory: Appointment | null = null;

  // SuperAdmin lists
  pendingDoctors: any[] = [];
  pendingClinics: any[] = [];
  pendingAdmins: any[] = [];

  // Doctor lists & states
  doctorClinics: any[] = [];
  selectedClinicIds: string[] = [];
  showClinicModal = false;
  showAdminModal = false;
  selectedClinicIdForAdmin = '';
  selectedClinicNameForAdmin = '';

  // Reject clinic states
  showRejectModal = false;
  selectedClinicIdForRejection = '';
  rejectionReason = '';

  // Weekday definitions
  weekDays = ['Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday', 'Sunday'];
  selectedDaysRegister: string[] = [];
  selectedDaysAdmin: string[] = [];

  // Split shift variables for Clinic Admin
  isSplitShiftAdmin = false;
  startTime1Admin = '';
  endTime1Admin = '';
  startTime2Admin = '';
  endTime2Admin = '';
  timingsErrorMessageAdmin = '';

  // Booking Window Calendar state (shared for admin and doctor edit modals)
  adminBookingCalMonth = new Date();
  adminBookingCalDays: any[] = [];
  adminBookingPickStart = '';
  adminBookingPickEnd = '';

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

  // Admin Clinic properties
  adminClinic: any = null;
  showAdminEditModal = false;
  adminClinicForm = {
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
    unavailabilityReason: '',
    isDoctorAvailable: true,
    doctorUnavailabilityReason: '',
    bookingWindowStartDate: '',
    bookingWindowEndDate: '',
    supportInPerson: true,
    supportVideo: false,
    maxAppointmentsPerDay: null as number | null
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
    private authService: AuthService,
    private appointmentService: AppointmentService,
    private adminService: AdminService,
    private patientService: PatientService,
    private toastService: ToastService,
    private notificationService: NotificationService,
    private route: ActivatedRoute
  ) { }

  ngOnInit(): void {
    this.role = this.authService.getRole() || 'Patient';
    this.firstName = sessionStorage.getItem('firstName') || 'User';
    this.historyMode = !!this.route.snapshot.data['historyOnly'];
    if (this.role === 'Doctor') {
      this.loadDoctorClinics();
    }
    this.loadDashboardData();

    // Listen for silent refresh signals to update the dashboard automatically in real-time
    this.signalrSub = this.notificationService.refreshData$.subscribe({
      next: (area) => {
        // Patients only refresh for appointment events
        if (this.role === 'Patient' && area !== 'Appointments') {
          return;
        }
        this.loadDashboardData();
      }
    });
  }

  ngOnDestroy(): void {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
  }

  loadDashboardData(): void {
    this.isDashboardLoading = true;
    if (this.role === 'Patient') {
      this.appointmentService.getPatientDashboard(this.statusFilter, this.historyMode, 1, 1000).subscribe({
        next: (res) => {
          let fetchedAppointments = res.items;
          if (!this.historyMode) {
            fetchedAppointments = fetchedAppointments.filter((a: any) => 
              ['Confirmed', 'Pending', 'RescheduleProposed'].includes(a.status)
            );
          }
          this.appointments = fetchedAppointments;
          this.isDashboardLoading = false;
        },
        error: () => {
          this.errorMessage = 'Failed to load patient appointments.';
          this.isDashboardLoading = false;
        }
      });
    } else if (this.role === 'SuperAdmin') {
      this.loadSuperAdminData();
    } else {
      // Doctor or Clinic Admin
      const filters: any = {};
      if (this.role === 'Doctor') {
        const todayStart = new Date();
        todayStart.setHours(0, 0, 0, 0);
        const todayEnd = new Date();
        todayEnd.setHours(23, 59, 59, 999);
        filters.startDate = todayStart.toISOString();
        filters.endDate = todayEnd.toISOString();
      } else {
        if (this.statusFilter) filters.status = this.statusFilter;
      }

      this.appointmentService.getAdminDoctorDashboard(filters, 1, 1000).subscribe({
        next: (res) => {
          if (this.role === 'Doctor') {
            // Sort: Confirmed (Active) first, sorted by StartTime.
            // Completed / Pending (No-show) at the end, sorted by StartTime.
            this.appointments = res.items.sort((a, b) => {
              const statusA = a.status === 'Confirmed' ? 0 : 1;
              const statusB = b.status === 'Confirmed' ? 0 : 1;
              if (statusA !== statusB) {
                return statusA - statusB;
              }
              return new Date(a.startTime).getTime() - new Date(b.startTime).getTime();
            });

            // Pre-populate input fields
            res.items.forEach(app => {
              this.commentInputs[app.appointmentId] = app.comment || '';
              this.reportInputs[app.appointmentId] = app.report || '';
            });
            this.appointments = res.items;
          }
          this.isDashboardLoading = false;
        },
        error: () => {
          this.errorMessage = 'Failed to load dashboard appointments.';
          this.isDashboardLoading = false;
        }
      });

      if (this.role === 'Doctor') {
        this.loadDoctorClinics();
        this.checkDoctorProfileCompleteness();
      }
      if (this.role === 'Admin') {
        this.loadAdminClinic();
      }
    }
  }

  checkDoctorProfileCompleteness(): void {
    this.patientService.getDoctorProfile().subscribe({
      next: (profile) => {
        // If state, city, pincode, or addressline1 are blank/empty, flag it as incomplete!
        if (!profile.state || !profile.city || !profile.pincode || !profile.addressline1) {
          this.isDoctorAddressIncomplete = true;
        }
      }
    });
  }

  get filteredAppointments(): Appointment[] {
    let list = this.appointments;
    
    // Clinic filtering for doctors
    if (this.role === 'Doctor' && this.selectedClinicIds.length > 0) {
      list = list.filter(app => app.clinicId && this.selectedClinicIds.includes(app.clinicId));
    }

    // Date filtering
    if (this.dateFilter) {
      list = list.filter(app => app.appointmentDate.startsWith(this.dateFilter));
    }
    
    // Consultation filtering
    if (this.consultationFilter) {
      list = list.filter(app => app.consultationType === this.consultationFilter);
    }
    return list;
  }

  get totalCount(): number {
    return this.filteredAppointments.length;
  }

  get paginatedAppointments(): Appointment[] {
    const list = this.filteredAppointments;
    if (this.role === 'Doctor') {
      const startIndex = (this.doctorPage - 1) * this.doctorSize;
      return list.slice(startIndex, startIndex + this.doctorSize);
    } else {
      const startIndex = (this.patientPage - 1) * this.patientSize;
      return list.slice(startIndex, startIndex + this.patientSize);
    }
  }

  loadSuperAdminData(): void {
    this.isSuperAdminLoading = true;
    this.adminService.getPendingDoctors().subscribe({
      next: (res) => this.pendingDoctors = res,
      error: () => this.errorMessage = 'Failed to load pending doctors.'
    });

    this.adminService.getPendingClinics().subscribe({
      next: (res) => this.pendingClinics = res,
      error: () => this.errorMessage = 'Failed to load pending clinics.'
    });

    this.adminService.getPendingAdmins().subscribe({
      next: (res) => {
        this.pendingAdmins = res;
        this.isSuperAdminLoading = false;
      },
      error: () => {
        this.errorMessage = 'Failed to load pending admins.';
        this.isSuperAdminLoading = false;
      }
    });
  }

  loadDoctorClinics(): void {
    this.isClinicsLoading = true;
    this.adminService.getDoctorClinics().subscribe({
      next: (res) => {
        this.doctorClinics = res;
        this.isClinicsLoading = false;
      },
      error: () => { 
        this.isClinicsLoading = false;
      }
    });
  }

  // SuperAdmin Verification Actions
  verifyDoctor(doctorId: string, status: string): void {
    this.adminService.verifyDoctor(doctorId, status).subscribe({
      next: () => {
        this.toastService.showSuccess(`Doctor verification status updated to '${status}'.`);
        this.loadSuperAdminData();
      },
      error: (err) => this.toastService.showError(err?.error?.detail || 'Failed to verify doctor.')
    });
  }

  verifyClinic(clinicId: string): void {
    this.adminService.verifyClinic(clinicId).subscribe({
      next: () => {
        this.toastService.showSuccess('Clinic verified successfully.');
        this.loadSuperAdminData();
      },
      error: (err) => this.toastService.showError(err?.error?.detail || 'Failed to verify clinic.')
    });
  }

  verifyAdmin(adminId: string): void {
    this.adminService.verifyAdmin(adminId).subscribe({
      next: () => {
        this.toastService.showSuccess('Clinic Admin verified successfully.');
        this.loadSuperAdminData();
      },
      error: (err) => this.toastService.showError(err?.error?.detail || 'Failed to verify clinic admin.')
    });
  }

  // Doctor Clinic Registration Action
  openClinicModal(): void {
    this.showClinicModal = true;
    this.errorMessage = '';
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

  closeClinicModal(): void {
    this.showClinicModal = false;
  }

  // Reschedule Propose Methods
  openRescheduleModal(appId: string): void {
    this.selectedRescheduleAppId = appId;
    this.rescheduleDate = '';
    this.rescheduleTime = '';
    this.rescheduleReason = '';
    this.showRescheduleModal = true;
  }

  validateRescheduleDate(): void {
    if (!this.rescheduleDate || !this.selectedRescheduleAppId) return;
    
    const app = this.appointments.find(a => a.appointmentId === this.selectedRescheduleAppId);
    if (!app) return;

    let clinic = null;
    if (this.role === 'Doctor') {
      clinic = this.doctorClinics.find(c => c.clinicId === app.clinicId);
    } else if (this.role === 'Admin') {
      clinic = this.adminClinic;
    }
    
    if (!clinic || !clinic.openDays) return;

    const selectedDate = new Date(this.rescheduleDate);
    const dayName = selectedDate.toLocaleDateString('en-US', { weekday: 'long' }).toLowerCase();
    const openDays = clinic.openDays.toLowerCase();

    if (!openDays.includes(dayName)) {
      this.toastService.showError(`The clinic is completely closed on ${selectedDate.toLocaleDateString('en-US', { weekday: 'long' })}s. Please select a configured Working Day or Reschedule-Only day.`);
      this.rescheduleDate = '';
    }
  }

  closeRescheduleModal(): void {
    this.showRescheduleModal = false;
    this.selectedRescheduleAppId = '';
  }

  submitReschedulePropose(): void {
    if (!this.rescheduleDate || !this.rescheduleReason) {
      this.toastService.showError('Date and Reason are required.');
      return;
    }

    const payload = {
      appointmentId: this.selectedRescheduleAppId,
      proposedDate: this.rescheduleDate,
      proposedTime: this.rescheduleTime ? `${this.rescheduleDate}T${this.rescheduleTime}:00` : null,
      reason: this.rescheduleReason
    };

    this.appointmentService.proposeReschedule(payload).subscribe({
      next: () => {
        this.toastService.showSuccess('Reschedule proposed successfully.');
        this.closeRescheduleModal();
        this.loadDashboardData();
      },
      error: (err: any) => {
        this.toastService.showError(err, 'Failed to propose reschedule.');
      }
    });
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

  submitClinicRegistration(): void {
    if (!this.validateClinicForm(this.clinicOnlyForm)) {
      return;
    }
    this.adminService.registerClinicOnly(this.clinicOnlyForm).subscribe({
      next: () => {
        this.toastService.showSuccess('Clinic registered successfully. Awaiting Super Admin verification.');
        this.closeClinicModal();
        this.loadDoctorClinics();
      },
      error: (err) => {
        const errorDetail = err?.error?.detail || 'Failed to register clinic.';
        this.toastService.showError(errorDetail);
      }
    });
  }

  // Doctor Admin Registration Action
  openAdminModal(clinicId: string, clinicName: string): void {
    this.showAdminModal = true;
    this.errorMessage = '';
    this.selectedClinicIdForAdmin = clinicId;
    this.selectedClinicNameForAdmin = clinicName;
    this.adminForm = {
      clinicId: clinicId,
      adminEmail: '',
      adminPassword: '',
      adminFirstName: '',
      adminLastName: '',
      adminMobileNo: ''
    };
  }

  closeAdminModal(): void {
    this.showAdminModal = false;
  }

  submitAdminRegistration(): void {
    if (!this.adminForm.clinicId) {
      this.errorMessage = 'Please select a clinic.';
      this.toastService.showError(this.errorMessage);
      return;
    }
    this.adminService.registerClinicAdmin(this.adminForm).subscribe({
      next: () => {
        this.toastService.showSuccess('Clinic Admin registered successfully. Awaiting Super Admin verification.');
        this.closeAdminModal();
        this.loadDoctorClinics();
      },
      error: (err) => {
        this.errorMessage = err?.error?.detail || 'Failed to register clinic admin.';
        this.toastService.showError(this.errorMessage);
      }
    });
  }

  getVerifiedClinicsWithoutAdmin(): any[] {
    return this.doctorClinics.filter(c => c.isVerified && !c.hasAdmin);
  }

  onFilterChange(status?: string): void {
    if (status !== undefined) {
      this.statusFilter = status;
    }
    this.patientPage = 1;
    this.doctorPage = 1;
    // We only need to reload data if the status filter changes, because date/consultation are filtered locally on the fetched page.
    this.loadDashboardData();
  }

  openCancelAppointmentConfirm(id: string): void {
    this.selectedAppIdForCancel = id;
    this.showCancelAppointmentConfirm = true;
  }

  closeCancelAppointmentConfirm(): void {
    this.selectedAppIdForCancel = '';
    this.showCancelAppointmentConfirm = false;
  }

  confirmCancelAppointment(): void {
    if (!this.selectedAppIdForCancel) return;
    this.appointmentService.cancelAppointment(this.selectedAppIdForCancel).subscribe({
      next: () => {
        this.toastService.showSuccess('Appointment has been cancelled successfully.');
        this.closeCancelAppointmentConfirm();
        this.loadDashboardData();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to cancel appointment.');
      }
    });
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'Pending': return 'badge badge-pending';
      case 'Confirmed': return 'badge badge-confirmed';
      case 'Cancelled': return 'badge badge-cancelled';
      case 'Rejected': return 'badge badge-cancelled';
      case 'Completed': return 'badge badge-completed';
      default: return 'badge';
    }
  }

  toggleClinicFilter(clinicId: string): void {
    const idx = this.selectedClinicIds.indexOf(clinicId);
    if (idx > -1) {
      this.selectedClinicIds.splice(idx, 1);
    } else {
      this.selectedClinicIds.push(clinicId);
    }
  }

  // Reject clinic methods
  openRejectClinicModal(clinicId: string): void {
    this.selectedClinicIdForRejection = clinicId;
    this.rejectionReason = '';
    this.showRejectModal = true;
  }

  closeRejectModal(): void {
    this.showRejectModal = false;
    this.selectedClinicIdForRejection = '';
    this.rejectionReason = '';
  }

  submitClinicRejection(): void {
    if (!this.selectedClinicIdForRejection || !this.rejectionReason.trim()) {
      this.toastService.showError('Please enter a rejection reason.');
      return;
    }

    this.adminService.rejectClinic(this.selectedClinicIdForRejection, this.rejectionReason).subscribe({
      next: () => {
        this.toastService.showSuccess('Clinic registration rejected successfully.');
        this.closeRejectModal();
        this.loadSuperAdminData();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to reject clinic.');
      }
    });
  }

  // Edit clinic methods (Admin only now)

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

  // Clinic Admin Helpers
  loadAdminClinic(): void {
    this.adminService.getAdminClinic().subscribe({
      next: (res) => {
        this.adminClinic = res;
      },
      error: (err) => {
        this.toastService.showError('Failed to load clinic details.');
      }
    });
  }

  openAdminEditClinicModal(): void {
    if (!this.adminClinic) return;
    this.selectedDaysAdmin = this.adminClinic.openDays ? this.adminClinic.openDays.split(',').map((d: string) => d.trim()) : [];

    const startTimeStr = this.adminClinic.startTime || '';
    const endTimeStr = this.adminClinic.endTime || '';

    if (startTimeStr.includes(',')) {
      this.isSplitShiftAdmin = true;
      this.startTime1Admin = startTimeStr.split(',')[0]?.trim() || '';
      this.startTime2Admin = startTimeStr.split(',')[1]?.trim() || '';
      this.endTime1Admin = endTimeStr.split(',')[0]?.trim() || '';
      this.endTime2Admin = endTimeStr.split(',')[1]?.trim() || '';
    } else {
      this.isSplitShiftAdmin = false;
      this.startTime1Admin = startTimeStr;
      this.endTime1Admin = endTimeStr;
      this.startTime2Admin = '';
      this.endTime2Admin = '';
    }

    this.adminClinicForm = {
      clinicName: this.adminClinic.clinicName,
      clinicType: this.adminClinic.clinicType,
      country: 'India',
      state: this.adminClinic.state,
      city: this.adminClinic.city,
      area: this.adminClinic.area || '',
      pincode: this.adminClinic.pincode || '',
      addressline1: this.adminClinic.addressline1 || '',
      addressline2: this.adminClinic.addressline2 || '',
      openDays: this.adminClinic.openDays || '',
      startTime: startTimeStr,
      endTime: endTimeStr,
      isAvailable: this.adminClinic.isAvailable !== false,
      unavailabilityReason: this.adminClinic.unavailabilityReason || '',
      isDoctorAvailable: this.adminClinic.isDoctorAvailable !== false,
      doctorUnavailabilityReason: this.adminClinic.doctorUnavailabilityReason || '',
      bookingWindowStartDate: this.adminClinic.bookingWindowStartDate ? this.adminClinic.bookingWindowStartDate.substring(0, 10) : '',
      bookingWindowEndDate: this.adminClinic.bookingWindowEndDate ? this.adminClinic.bookingWindowEndDate.substring(0, 10) : '',
      supportInPerson: !this.adminClinic.supportedModes || this.adminClinic.supportedModes.includes('InPerson'),
      supportVideo: this.adminClinic.supportedModes ? this.adminClinic.supportedModes.includes('VideoConsultation') : false,
      maxAppointmentsPerDay: this.adminClinic.maxAppointmentsPerDay ?? null
    };
    this.adminBookingPickStart = this.adminClinicForm.bookingWindowStartDate;
    this.adminBookingPickEnd = this.adminClinicForm.bookingWindowEndDate;
    this.adminBookingCalMonth = this.adminBookingPickStart ? new Date(this.adminBookingPickStart) : new Date();
    this.generateAdminBookingCalendar();
    this.showAdminEditModal = true;
  }

  closeAdminEditClinicModal(): void {
    this.showAdminEditModal = false;
    this.selectedDaysAdmin = [];
    this.isSplitShiftAdmin = false;
    this.startTime1Admin = '';
    this.endTime1Admin = '';
    this.startTime2Admin = '';
    this.endTime2Admin = '';
    this.timingsErrorMessageAdmin = '';
    this.adminClinicForm.bookingWindowStartDate = '';
    this.adminClinicForm.bookingWindowEndDate = '';
  }

  toggleDayAdmin(day: string): void {
    const index = this.selectedDaysAdmin.indexOf(day);
    if (index > -1) {
      this.selectedDaysAdmin.splice(index, 1);
    } else {
      this.selectedDaysAdmin.push(day);
    }
    this.selectedDaysAdmin.sort((a, b) => this.weekDays.indexOf(a) - this.weekDays.indexOf(b));
    this.adminClinicForm.openDays = this.selectedDaysAdmin.join(',');
    // Regenerate booking calendar and clear range when days change
    this.adminBookingPickStart = '';
    this.adminBookingPickEnd = '';
    this.adminClinicForm.bookingWindowStartDate = '';
    this.adminClinicForm.bookingWindowEndDate = '';
    this.generateAdminBookingCalendar();
  }

  isDaySelectedAdmin(day: string): boolean {
    return this.selectedDaysAdmin.includes(day);
  }

  // ─── Booking Window Calendar: Admin ───────────────────────────────────────

  generateAdminBookingCalendar(): void {
    this.adminBookingCalDays = this.generateBookingCalendarDays(
      this.adminBookingCalMonth,
      this.selectedDaysAdmin,
      this.adminBookingPickStart,
      this.adminBookingPickEnd
    );
  }

  adminBookingCalPrev(): void {
    const m = this.adminBookingCalMonth.getMonth();
    this.adminBookingCalMonth = new Date(this.adminBookingCalMonth.getFullYear(), m - 1, 1);
    this.generateAdminBookingCalendar();
  }

  adminBookingCalNext(): void {
    const m = this.adminBookingCalMonth.getMonth();
    this.adminBookingCalMonth = new Date(this.adminBookingCalMonth.getFullYear(), m + 1, 1);
    this.generateAdminBookingCalendar();
  }

  onAdminBookingDayClick(day: any): void {
    if (!day.isOpenDay || day.dayNumber === null) return;
    const clicked = day.dateString as string;
    if (!this.adminBookingPickStart || (this.adminBookingPickStart && this.adminBookingPickEnd)) {
      // Reset: start a new selection
      this.adminBookingPickStart = clicked;
      this.adminBookingPickEnd = '';
    } else {
      // Second click: set end (swap if before start)
      if (clicked < this.adminBookingPickStart) {
        this.adminBookingPickEnd = this.adminBookingPickStart;
        this.adminBookingPickStart = clicked;
      } else {
        this.adminBookingPickEnd = clicked;
      }
    }
    this.adminClinicForm.bookingWindowStartDate = this.adminBookingPickStart;
    this.adminClinicForm.bookingWindowEndDate = this.adminBookingPickEnd;
    this.generateAdminBookingCalendar();
  }

  clearAdminBookingWindow(): void {
    this.adminBookingPickStart = '';
    this.adminBookingPickEnd = '';
    this.adminClinicForm.bookingWindowStartDate = '';
    this.adminClinicForm.bookingWindowEndDate = '';
    this.generateAdminBookingCalendar();
  }

  getAdminBookingCalMonthName(): string {
    return this.adminBookingCalMonth.toLocaleString('default', { month: 'long', year: 'numeric' });
  }


  // ─── Shared calendar day-grid generator ───────────────────────────────────

  generateBookingCalendarDays(
    currentMonth: Date,
    openDayNames: string[],
    pickStart: string,
    pickEnd: string
  ): any[] {
    const year = currentMonth.getFullYear();
    const month = currentMonth.getMonth();
    const firstDay = new Date(year, month, 1);
    const totalDays = new Date(year, month + 1, 0).getDate();
    const startDow = firstDay.getDay(); // 0=Sun
    const openNorm = openDayNames.map(d => d.toLowerCase());
    const fullWeek = ['sunday', 'monday', 'tuesday', 'wednesday', 'thursday', 'friday', 'saturday'];

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const days: any[] = [];
    // Padding
    for (let i = 0; i < startDow; i++) {
      days.push({ dayNumber: null, dateString: '', isOpenDay: false, inRange: false, isStart: false, isEnd: false });
    }

    for (let d = 1; d <= totalDays; d++) {
      const dateObj = new Date(year, month, d);
      dateObj.setHours(0, 0, 0, 0);
      const yyyy = dateObj.getFullYear();
      const mm = String(dateObj.getMonth() + 1).padStart(2, '0');
      const dd = String(dateObj.getDate()).padStart(2, '0');
      const dateString = `${yyyy}-${mm}-${dd}`;
      const dayName = fullWeek[dateObj.getDay()];
      const isOpenDay = openNorm.includes(dayName) && dateObj >= today;
      const isStart = dateString === pickStart;
      const isEnd = dateString === pickEnd;
      const inRange = pickStart && pickEnd ? dateString > pickStart && dateString < pickEnd : false;
      days.push({ dayNumber: d, dateString, isOpenDay, inRange, isStart, isEnd, isToday: dateObj.getTime() === today.getTime() });
    }
    return days;
  }

  submitAdminClinicEdit(): void {
    if (!this.validateClinicForm(this.adminClinicForm)) {
      return;
    }

    if (this.isSplitShiftAdmin) {
      if (!this.startTime1Admin || !this.endTime1Admin || !this.startTime2Admin || !this.endTime2Admin) {
        this.toastService.showError('Please configure both timing shifts completely.');
        return;
      }
      if (this.startTime1Admin >= this.endTime1Admin) {
        this.toastService.showError('Shift 1 opening time must be before closing time.');
        return;
      }
      if (this.startTime2Admin >= this.endTime2Admin) {
        this.toastService.showError('Shift 2 opening time must be before closing time.');
        return;
      }
      if (this.endTime1Admin > this.startTime2Admin) {
        this.toastService.showError('Shift 1 closing time cannot be after Shift 2 opening time.');
        return;
      }
      this.adminClinicForm.startTime = `${this.startTime1Admin},${this.startTime2Admin}`;
      this.adminClinicForm.endTime = `${this.endTime1Admin},${this.endTime2Admin}`;
    } else {
      if (!this.startTime1Admin || !this.endTime1Admin) {
        this.toastService.showError('Please configure opening and closing hours.');
        return;
      }
      if (this.startTime1Admin >= this.endTime1Admin) {
        this.toastService.showError('Opening time must be before closing time.');
        return;
      }
      this.adminClinicForm.startTime = this.startTime1Admin;
      this.adminClinicForm.endTime = this.endTime1Admin;
    }

    if (this.adminClinicForm.isAvailable) {
      if (!this.adminClinicForm.openDays || !this.adminClinicForm.startTime || !this.adminClinicForm.endTime) {
        this.toastService.showError('Active/Open clinics must have a timing schedule (open days, start time, and end time) configured.');
        return;
      }
    }

    const modesList: string[] = [];
    if (this.adminClinicForm.supportInPerson) modesList.push('InPerson');
    if (this.adminClinicForm.supportVideo) modesList.push('VideoConsultation');
    const supportedModesStr = modesList.join(',');

    const payload = {
      ...this.adminClinicForm,
      bookingWindowStartDate: this.adminClinicForm.bookingWindowStartDate ? new Date(this.adminClinicForm.bookingWindowStartDate).toISOString() : null,
      bookingWindowEndDate: this.adminClinicForm.bookingWindowEndDate ? new Date(this.adminClinicForm.bookingWindowEndDate).toISOString() : null,
      supportedModes: supportedModesStr
    };

    this.adminService.updateClinicByAdmin(payload).subscribe({
      next: () => {
        this.toastService.showSuccess('Clinic details updated successfully.');
        this.closeAdminEditClinicModal();
        this.loadAdminClinic();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to update clinic details.');
      }
    });
  }

  validateTimingsChangeAdmin(): void {
    this.timingsErrorMessageAdmin = '';

    if (this.isSplitShiftAdmin) {
      if (this.startTime1Admin && this.endTime1Admin && this.startTime1Admin >= this.endTime1Admin) {
        this.timingsErrorMessageAdmin = 'Session 1 opening time must be before closing time.';
        return;
      }
      if (this.startTime2Admin && this.endTime2Admin && this.startTime2Admin >= this.endTime2Admin) {
        this.timingsErrorMessageAdmin = 'Session 2 opening time must be before closing time.';
        return;
      }
      if (this.endTime1Admin && this.startTime2Admin && this.endTime1Admin > this.startTime2Admin) {
        this.timingsErrorMessageAdmin = 'Session 1 closing time cannot be after Session 2 opening time.';
        return;
      }
    } else {
      if (this.startTime1Admin && this.endTime1Admin && this.startTime1Admin >= this.endTime1Admin) {
        this.timingsErrorMessageAdmin = 'Opening time must be before closing time.';
        return;
      }
    }
  }

  getSortedDays(openDaysStr: string): string[] {
    if (!openDaysStr) return [];
    const days = openDaysStr.split(',').map(d => d.trim());
    return days.sort((a, b) => this.weekDays.indexOf(a) - this.weekDays.indexOf(b));
  }

  toggleNotesRow(appId: string): void {
    this.expandedNoteRows[appId] = !this.expandedNoteRows[appId];
  }

  openCompleteConfirm(appId: string): void {
    this.selectedAppIdForComplete = appId;
    this.showCompleteConfirm = true;
  }

  closeCompleteConfirm(): void {
    this.selectedAppIdForComplete = '';
    this.showCompleteConfirm = false;
  }

  confirmComplete(): void {
    if (!this.selectedAppIdForComplete) return;
    const comment = this.commentInputs[this.selectedAppIdForComplete] || '';
    const report = this.reportInputs[this.selectedAppIdForComplete] || '';

    this.appointmentService.completeAppointment(this.selectedAppIdForComplete, comment, report).subscribe({
      next: () => {
        this.toastService.showSuccess('Appointment has been marked as completed.');
        this.closeCompleteConfirm();
        this.loadDashboardData();
      },
      error: (err) => {
        this.toastService.showError(err, 'Failed to complete appointment.');
      }
    });
  }

  openNoShowConfirm(appId: string): void {
    this.selectedAppIdForNoShow = appId;
    this.showNoShowConfirm = true;
  }

  closeNoShowConfirm(): void {
    this.selectedAppIdForNoShow = '';
    this.showNoShowConfirm = false;
  }

  confirmNoShow(): void {
    if (!this.selectedAppIdForNoShow) return;
    const comment = this.commentInputs[this.selectedAppIdForNoShow] || '';

    this.appointmentService.movePendingAppointment(this.selectedAppIdForNoShow, comment).subscribe({
      next: () => {
        this.toastService.showSuccess('Appointment has been set to Pending (Absent).');
        this.closeNoShowConfirm();
        this.loadDashboardData();
      },
      error: (err) => {
        this.toastService.showError(err, 'Failed to mark appointment as pending.');
      }
    });
  }

  // Reject Logic
  openRejectConfirm(appId: string): void {
    this.selectedAppIdForReject = appId;
    this.rejectReasonInput = '';
    this.showRejectConfirm = true;
  }

  closeRejectConfirm(): void {
    this.selectedAppIdForReject = '';
    this.rejectReasonInput = '';
    this.showRejectConfirm = false;
  }

  submitReject(): void {
    if (!this.selectedAppIdForReject || !this.rejectReasonInput.trim()) {
      this.toastService.showError('Please provide a reason for rejection.');
      return;
    }

    this.appointmentService.rejectAppointment(this.selectedAppIdForReject, this.rejectReasonInput).subscribe({
      next: () => {
        this.toastService.showSuccess('Appointment rejected successfully.');
        this.closeRejectConfirm();
        this.loadDashboardData();
      },
      error: (err) => {
        this.toastService.showError(err, 'Failed to reject appointment.');
      }
    });
  }

  selectedAppDateForAssignTime = '';

  openAssignTimeModal(appId: string, date: string): void {
    this.selectedAppIdForAssignTime = appId;
    this.selectedAppDateForAssignTime = new Date(date).toISOString().split('T')[0];
    this.assignedTimeInput = '';
    this.showAssignTimeModal = true;
  }

  closeAssignTimeModal(): void {
    this.selectedAppIdForAssignTime = '';
    this.selectedAppDateForAssignTime = '';
    this.showAssignTimeModal = false;
  }

  submitAssignTime(): void {
    if (!this.selectedAppIdForAssignTime || !this.assignedTimeInput) {
      this.toastService.showError('Please select a valid time.');
      return;
    }

    const comment = this.commentInputs[this.selectedAppIdForAssignTime] || '';
    
    // Combine the date and time strings without converting to UTC
    let formattedTime = `${this.selectedAppDateForAssignTime}T${this.assignedTimeInput}`;
    if (this.assignedTimeInput.length === 5) { // HH:mm
      formattedTime += ':00';
    }

    this.appointmentService.assignAppointmentTime(this.selectedAppIdForAssignTime, formattedTime, comment).subscribe({
      next: () => {
        this.toastService.showSuccess('Time assigned and appointment confirmed.');
        this.closeAssignTimeModal();
        this.loadDashboardData();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to assign time.');
      }
    });
  }

  doctorPrevPage(): void {
    if (this.doctorPage > 1) {
      this.doctorPage--;
    }
  }

  doctorNextPage(): void {
    if (this.doctorPage * this.doctorSize < this.totalCount) {
      this.doctorPage++;
    }
  }

  doctorTotalPages(): number {
    return Math.ceil(this.totalCount / this.doctorSize) || 1;
  }

  acceptReschedule(appId: string): void {
    this.appointmentService.respondReschedule({ appointmentId: appId, accept: true }).subscribe({
      next: () => {
        this.toastService.showSuccess('Appointment reschedule accepted successfully.');
        this.loadDashboardData();
      },
      error: (err) => {
        this.toastService.showError(err, 'Failed to accept reschedule.');
      }
    });
  }

  rejectReschedule(appId: string): void {
    this.appointmentService.respondReschedule({ appointmentId: appId, accept: false }).subscribe({
      next: () => {
        this.toastService.showSuccess('Appointment reschedule rejected. The appointment is now cancelled.');
        this.loadDashboardData();
      },
      error: (err) => {
        this.toastService.showError(err, 'Failed to reject reschedule.');
      }
    });
  }

  openPatientDetailsModal(patientId: string): void {
    this.selectedPatientDetails = null;
    this.showPatientDetailsModal = true;
    this.isDetailsLoading = true;

    this.appointmentService.getPatientDetails(patientId).subscribe({
      next: (res) => {
        this.selectedPatientDetails = res;
        this.isDetailsLoading = false;
      },
      error: (err) => {
        this.toastService.showError(err, 'Failed to fetch patient details.');
        this.isDetailsLoading = false;
        this.closePatientDetailsModal();
      }
    });
  }

  closePatientDetailsModal(): void {
    this.showPatientDetailsModal = false;
    this.selectedPatientDetails = null;
  }

  getAge(dob: string | Date | undefined): number {
    if (!dob) return 0;
    const birthDate = new Date(dob);
    const today = new Date();
    let age = today.getFullYear() - birthDate.getFullYear();
    const m = today.getMonth() - birthDate.getMonth();
    if (m < 0 || (m === 0 && today.getDate() < birthDate.getDate())) {
      age--;
    }
    return age;
  }

  openHistoryModal(patientId: string, patientName: string): void {
    this.selectedPatientName = patientName;
    this.patientHistory = [];
    this.showHistoryModal = true;
    this.isHistoryLoading = true;

    this.appointmentService.getAdminDoctorDashboard({ patientId: patientId }, 1, 100).subscribe({
      next: (res) => {
        this.patientHistory = res.items.sort((a, b) => new Date(b.appointmentDate).getTime() - new Date(a.appointmentDate).getTime());
        this.isHistoryLoading = false;
      },
      error: (err) => {
        this.toastService.showError(err, 'Failed to retrieve patient medical history.');
        this.isHistoryLoading = false;
        this.closeHistoryModal();
      }
    });
  }

  closeHistoryModal(): void {
    this.showHistoryModal = false;
    this.selectedPatientName = '';
    this.patientHistory = [];
  }

  openPatientHistoryModal(app: Appointment): void {
    this.selectedAppForHistory = app;
    this.showPatientHistoryModal = true;
  }

  closePatientHistoryModal(): void {
    this.showPatientHistoryModal = false;
    this.selectedAppForHistory = null;
  }

  openClinicDetailsModal(clinic: any): void {
    this.selectedClinicDetails = clinic;
    this.showClinicDetailsModal = true;
  }

  closeClinicDetailsModal(): void {
    this.showClinicDetailsModal = false;
    this.selectedClinicDetails = null;
  }

  getStatusColor(status: string): string {
    switch (status) {
      case 'Confirmed': return '#10b981';
      case 'Pending': return '#f59e0b';
      case 'Cancelled': return '#ef4444';
      case 'Rejected': return '#dc2626';
      case 'Completed': return '#8b5cf6';
      case 'RescheduleProposed': return '#ec4899';
      default: return '#6b7280';
    }
  }

  getStatusTitle(status: string): string {
    switch (status) {
      case 'Confirmed': return 'Confirmed';
      case 'Pending': return 'Confirmation Pending';
      case 'Cancelled': return 'Cancelled';
      case 'Rejected': return 'Rejected';
      case 'Completed': return 'Completed';
      case 'RescheduleProposed': return 'Reschedule Proposed';
      default: return status;
    }
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

  getConsultationTypeLabel(type: string): string {
    return type === 'InPerson' ? '🏠 In-Person Visit' : '🎥 Video Consultation';
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
}
