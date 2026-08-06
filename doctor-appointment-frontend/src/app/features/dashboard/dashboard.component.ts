import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { forkJoin, Subscription } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { AppointmentService } from '../../core/services/appointment.service';
import { AdminService } from '../../core/services/admin.service';
import { PatientService } from '../../core/services/patient.service';
import { ToastService } from '../../core/services/toast.service';
import { NotificationService } from '../../core/services/notification.service';
import { Appointment } from '../../core/models/appointment.model';
import { ActivatedRoute, Router } from '@angular/router';

@Component({
  selector: 'app-dashboard',
  templateUrl: './dashboard.component.html',
  styleUrls: ['./dashboard.component.css']
})
export class DashboardComponent implements OnInit, OnDestroy {
  role: any = '';
  Math = Math;
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
  todayDate = new Date().toISOString().split('T')[0];

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

  isFollowUpChecked = false;
  followUpClinicId = '';
  followUpDate = '';
  followUpTime = '';
  followUpConsultationType = 'InPerson';
  followUpDateError = '';
  isLoadingFollowUpAvailability = false;

  // Custom Calendar properties
  followUpCurrentMonth: Date = new Date();
  followUpCalendarDays: any[] = [];
  weekDaysList: string[] = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];

  todayDateObj = new Date();
  todayMonth = new Date().toLocaleDateString('en-US', { month: 'short' }).toUpperCase();
  todayDayNumber = new Date().getDate();
  todayDayName = new Date().toLocaleDateString('en-US', { weekday: 'long' });
  todayYear = new Date().getFullYear();

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

  // Doctor metrics
  get todaysTotal() {
    return this.appointments.filter(a => !['Cancelled', 'Rejected', 'RescheduleProposed'].includes(a.status)).length;
  }

  get todaysCompleted() {
    return this.appointments.filter(a => a.status === 'Completed').length;
  }

  get todaysRemaining() {
    return this.appointments.filter(a => !['Completed', 'Cancelled', 'Rejected', 'RescheduleProposed'].includes(a.status)).length;
  }

  get currentDoctorActivePatient(): Appointment | null {
    if (this.role !== 'Doctor' || !this.appointments || this.appointments.length === 0) return null;
    const active = this.appointments.filter(a => a.status === 'Confirmed');
    return active.length > 0 ? active[0] : null;
  }

  get doctorPendingRequestsCount(): number {
    if (this.role !== 'Doctor' || !this.appointments) return 0;
    return this.appointments.filter(a => a.status === 'Pending').length;
  }

  // Patient metrics
  isPatientStatsLoading = true;
  patientTotalCompleted = 0;
  patientTotalUpcoming = 0; // Excludes today
  patientTotalPending = 0;

  get todayRemainingAppointments(): Appointment[] {
    if (this.role !== 'Patient' || !this.appointments || this.appointments.length === 0) return [];
    const todayStr = new Date().toISOString().split('T')[0];
    return this.appointments.filter(a => {
      if (!a.appointmentDate) return false;
      const isToday = a.appointmentDate.startsWith(todayStr);
      const isActive = a.status === 'Confirmed' || a.status === 'Pending' || a.status === 'RescheduleProposed' || a.status === 'FollowUpProposed';
      return isToday && isActive;
    });
  }

  get todayAppointment(): Appointment | null {
    const list = this.todayRemainingAppointments;
    return list.length > 0 ? list[0] : null;
  }

  get remainingTodayCount(): number {
    return this.todayRemainingAppointments.length;
  }

  get nextUpcomingAppointment(): Appointment | null {
    if (this.role !== 'Patient' || !this.appointments || this.appointments.length === 0) return null;
    const todayStr = new Date().toISOString().split('T')[0];
    const upcoming = this.appointments.filter(a => {
      if (a.status !== 'Confirmed' && a.status !== 'Pending' && a.status !== 'RescheduleProposed') return false;
      // Exclude today's visits if there are remaining today visits displayed in the Today card
      const isToday = a.appointmentDate && a.appointmentDate.startsWith(todayStr);
      return !isToday;
    });
    return upcoming.length > 0 ? upcoming[0] : null;
  }

  get twentyFourHourAppointment(): Appointment | null {
    if (this.role !== 'Patient' || !this.appointments || this.appointments.length === 0) return null;
    const now = new Date().getTime();
    const twentyFourHoursLater = now + (24 * 60 * 60 * 1000);
    const upcoming24h = this.appointments.filter(a => {
      if (a.status !== 'Confirmed') return false;
      const appTime = new Date(a.appointmentDate).getTime();
      return appTime >= now && appTime <= twentyFourHoursLater;
    });
    return upcoming24h.length > 0 ? upcoming24h[0] : null;
  }

  onPatientStatClick(statusFilter: string): void {
    this.statusFilter = statusFilter;
    this.onFilterChange(statusFilter);
    const tableEl = document.querySelector('.table-card');
    if (tableEl) {
      tableEl.scrollIntoView({ behavior: 'smooth' });
    }
  }

  bookFollowUp(app: Appointment): void {
    if (app.doctorId) {
      this.router.navigate(['/patient/book-appointment'], {
        queryParams: {
          doctorId: app.doctorId,
          clinicId: app.clinicId || '',
          isFollowUp: true,
          previousVisitDate: app.appointmentDate || ''
        }
      });
    }
  }

  // Patient Details Modal States
  showPatientDetailsModal = false;
  selectedPatientDetails: any = null;
  isDetailsLoading = false;

  // View Doctor Details Modal (Patient Portal)
  showDoctorDetailsModal = false;
  selectedDoctorDetails: any = null;
  isDoctorDetailsLoading = false;

  // Clinic Details Modal States
  showClinicDetailsModal = false;
  selectedClinicDetails: any = null;

  // Doctor Dashboard Patient History modal states
  showHistoryModal = false;
  selectedPatientName = '';
  patientHistory: Appointment[] = [];
  isHistoryLoading = false;
  historyClinicFilters: { [clinicName: string]: boolean } = {};
  historyStatusFilters: { [statusName: string]: boolean } = {};

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
    private route: ActivatedRoute,
    private router: Router
  ) { }

  ngOnInit(): void {
    this.role = this.authService.getRole() || 'Patient';
    this.firstName = this.authService.getFirstName();
    this.historyMode = !!this.route.snapshot.data['historyOnly'];
    if (this.role === 'Doctor') {
      this.loadDoctorClinics();
    }
    this.loadDashboardData();

    // Set default view mode: 'card' for small screen, 'table' for desktop
    if (typeof window !== 'undefined') {
      this.appointmentViewMode = window.innerWidth <= 768 ? 'card' : 'table';
    }

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
          if (!this.historyMode) {
            // Dashboard Upcoming widget: show active/upcoming appointments
            // Filter out Cancelled & Rejected (they belong on the Appointment History page)
            this.appointments = res.items.filter(a => a.status !== 'Cancelled' && a.status !== 'Rejected');
          } else {
            this.appointments = res.items;
          }
          this.isDashboardLoading = false;
        },
        error: () => {
          this.errorMessage = 'Failed to load patient appointments.';
          this.isDashboardLoading = false;
        }
      });
      // Load all appointments to compute stats (both history and upcoming)
      this.loadPatientStats();
    } else if (this.role === 'SuperAdmin') {
      this.loadSuperAdminData();
    } else {
      // Doctor or Clinic Admin
      const filters: any = {};
      if (this.role === 'Doctor') {
        const today = new Date();
        const year = today.getFullYear();
        const month = String(today.getMonth() + 1).padStart(2, '0');
        const day = String(today.getDate()).padStart(2, '0');
        const todayString = `${year}-${month}-${day}`;
        filters.startDate = todayString;
        filters.endDate = todayString;
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
          this.appointments = [];
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

  loadPatientStats(): void {
    const todayStr = new Date().toISOString().split('T')[0];

    // Fetch upcoming and history simultaneously
    this.isPatientStatsLoading = true;
    forkJoin({
      upcomingRes: this.appointmentService.getPatientDashboard('', false, 1, 1000),
      historyRes: this.appointmentService.getPatientDashboard('', true, 1, 1000)
    }).subscribe({
      next: ({ upcomingRes, historyRes }) => {
        const allApps = [...upcomingRes.items, ...historyRes.items];

        // Completed: Any appointment marked as completed, whether today or in the past
        this.patientTotalCompleted = allApps.filter(a => a.status === 'Completed').length;

        // Pending: Any appointment waiting for confirmation
        this.patientTotalPending = allApps.filter(a => a.status === 'Pending').length;

        // Upcoming: Any active/pending appointment that is specifically AFTER today
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

  checkDoctorProfileCompleteness(): void {
    this.patientService.getDoctorProfile().subscribe({
      next: (profile) => {
        if (profile.firstName) {
          this.firstName = profile.firstName;
          this.authService.updateCachedFirstName(profile.firstName, 'Doctor');
        }
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
    const startIndex = (this.patientPage - 1) * this.patientSize;
    return list.slice(startIndex, startIndex + this.patientSize);
  }

  get totalPages(): number {
    return Math.ceil(this.totalCount / this.patientSize) || 1;
  }

  get currentPage(): number {
    return this.patientPage;
  }

  onPageChange(page: number): void {
    if (page < 1 || page > this.totalPages) return;
    this.patientPage = page;
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

  appointmentViewMode: 'table' | 'card' = 'table';

  markAsCompleted(appId: string): void {
    this.appointmentService.completeAppointment(appId).subscribe({
      next: () => {
        this.toastService.showSuccess('Appointment marked as Completed!');
        this.loadDashboardData();
      },
      error: (err: any) => this.toastService.showError(err, 'Failed to complete appointment')
    });
  }

  bookAgain(app: any): void {
    if (app?.doctorId) {
      this.router.navigate(['/patient/book-appointment'], {
        queryParams: { doctorId: app.doctorId, clinicId: app.clinicId || '' }
      });
    }
  }

  validateRescheduleDate(): void {
    if (!this.rescheduleDate || !this.selectedRescheduleAppId) return;

    const selectedDate = new Date(this.rescheduleDate);
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    if (selectedDate < today) {
      this.toastService.showError('Proposed reschedule date cannot be in the past.');
      this.rescheduleDate = '';
      return;
    }

    const app = this.appointments.find(a => a.appointmentId === this.selectedRescheduleAppId);
    if (!app) return;

    let clinic = null;
    if (this.role === 'Doctor') {
      clinic = this.doctorClinics.find(c => c.clinicId === app.clinicId);
    } else if (this.role === 'Admin') {
      clinic = this.adminClinic;
    }

    if (!clinic) return;

    // 1. Booking Window Date Validation
    if (clinic.bookingWindowStartDate) {
      const windowStart = new Date(clinic.bookingWindowStartDate);
      windowStart.setHours(0, 0, 0, 0);
      if (selectedDate < windowStart) {
        this.toastService.showError(`Selected date is before the clinic's active booking window start date (${windowStart.toLocaleDateString()}).`);
        this.rescheduleDate = '';
        return;
      }
    }

    if (clinic.bookingWindowEndDate) {
      const windowEnd = new Date(clinic.bookingWindowEndDate);
      windowEnd.setHours(23, 59, 59, 999);
      if (selectedDate > windowEnd) {
        this.toastService.showError(`Selected date exceeds the clinic's active booking window end date (${windowEnd.toLocaleDateString()}).`);
        this.rescheduleDate = '';
        return;
      }
    }

    // 2. Open Days / Reschedule Days Validation
    if (clinic.openDays) {
      const dayName = selectedDate.toLocaleDateString('en-US', { weekday: 'long' }).toLowerCase();
      const openDaysLower = clinic.openDays.toLowerCase();

      const isNormalOpen = openDaysLower.includes(dayName);
      const isRescheduleOpen = openDaysLower.includes(`[reschedule:${dayName}]`);

      if (!isNormalOpen && !isRescheduleOpen) {
        this.toastService.showError(`The clinic is closed on ${selectedDate.toLocaleDateString('en-US', { weekday: 'long' })}s. Please select an allowed Working Day or Reschedule-Only day.`);
        this.rescheduleDate = '';
      }
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
      case 'RescheduleProposed': return 'badge badge-pending';
      case 'Confirmed': return 'badge badge-confirmed';
      case 'Cancelled': return 'badge badge-cancelled';
      case 'Rejected': return 'badge badge-cancelled';
      case 'Completed': return 'badge badge-completed';
      default: return 'badge';
    }
  }

  formatHistoryStatus(status: string): string {
    if (status === 'RescheduleProposed') return 'RESCHEDULING';
    return status;
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

    // Reset follow-up form state
    this.isFollowUpChecked = false;
    this.followUpDate = '';
    this.followUpTime = '';
    this.followUpConsultationType = 'InPerson';
    this.followUpDateError = '';
    this.isLoadingFollowUpAvailability = false;

    // Populate default values from selected appointment
    const app = this.appointments.find(a => a.appointmentId === appId);
    if (app) {
      this.followUpClinicId = app.clinicId || '';
      this.followUpConsultationType = app.consultationType || 'InPerson';
    }

    // Initialize calendar
    this.followUpCurrentMonth = new Date();
    this.generateFollowUpCalendar();
  }

  closeCompleteConfirm(): void {
    this.selectedAppIdForComplete = '';
    this.showCompleteConfirm = false;
  }

  confirmComplete(): void {
    if (!this.selectedAppIdForComplete) return;
    const comment = this.commentInputs[this.selectedAppIdForComplete] || '';
    const report = this.reportInputs[this.selectedAppIdForComplete] || '';

    let followUpPayload: any = null;
    if (this.isFollowUpChecked) {
      if (this.followUpDateError) {
        this.toastService.showError(this.followUpDateError);
        return;
      }
      if (!this.followUpClinicId || !this.followUpDate || !this.followUpTime) {
        this.toastService.showError('Please configure all follow-up appointment details (Date and Time).');
        return;
      }
      followUpPayload = {
        clinicId: this.followUpClinicId,
        appointmentDate: this.followUpDate,
        startTime: this.followUpTime,
        endTime: this.followUpTime,
        consultationType: this.followUpConsultationType
      };
    }

    this.appointmentService.completeAppointment(this.selectedAppIdForComplete, comment, report, followUpPayload).subscribe({
      next: () => {
        this.toastService.showSuccess(this.isFollowUpChecked ? 'Appointment marked as completed & follow-up proposed.' : 'Appointment marked as completed.');
        this.closeCompleteConfirm();
        this.loadDashboardData();
      },
      error: (err) => {
        this.toastService.showError(err, 'Failed to complete appointment.');
      }
    });
  }

  getTodayDateString(): string {
    const today = new Date();
    const yyyy = today.getFullYear();
    const mm = String(today.getMonth() + 1).padStart(2, '0');
    const dd = String(today.getDate()).padStart(2, '0');
    return `${yyyy}-${mm}-${dd}`;
  }

  validateFollowUpDate(): void {
    if (!this.followUpDate) {
      this.followUpDateError = '';
      return;
    }

    const todayStr = this.getTodayDateString();
    if (this.followUpDate < todayStr) {
      this.followUpDateError = 'Follow-up date cannot be in the past.';
      this.followUpDate = '';
      this.toastService.showError(this.followUpDateError);
      return;
    }

    const dateObj = new Date(this.followUpDate + 'T00:00:00');
    const weekDays = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
    const dayName = weekDays[dateObj.getDay()];

    const clinic = this.doctorClinics.find(c => c.clinicId === this.followUpClinicId);
    if (clinic) {
      const openDaysArray = clinic.openDays ? clinic.openDays.split(',').map((d: string) => d.trim()) : [];
      if (openDaysArray.length > 0 && !openDaysArray.includes(dayName)) {
        this.followUpDateError = `Clinic is closed on ${dayName}. Open days: ${clinic.openDays}`;
        this.followUpDate = '';
        this.toastService.showError(this.followUpDateError);
        return;
      }
    }

    this.followUpDateError = '';
    this.isLoadingFollowUpAvailability = true;
    this.appointmentService.getDayAvailability(this.followUpClinicId, this.followUpDate).subscribe({
      next: (avail) => {
        this.isLoadingFollowUpAvailability = false;
        if (avail.isFull) {
          this.followUpDateError = `This date is fully booked (${avail.bookedCount}/${avail.maxCapacity} appointments). Please choose another date.`;
          this.followUpDate = '';
          this.toastService.showError(this.followUpDateError);
        }
      },
      error: () => {
        this.isLoadingFollowUpAvailability = false;
      }
    });
  }

  onFollowUpClinicChange(): void {
    this.followUpDate = '';
    this.followUpDateError = '';
    this.generateFollowUpCalendar();
  }

  generateFollowUpCalendar(): void {
    const year = this.followUpCurrentMonth.getFullYear();
    const month = this.followUpCurrentMonth.getMonth();

    const firstDayIndex = new Date(year, month, 1).getDay();
    const totalDays = new Date(year, month + 1, 0).getDate();

    const days: any[] = [];

    // Add empty padding days for week start alignment
    for (let i = 0; i < firstDayIndex; i++) {
      days.push({ dayNumber: null, dateString: '', isAvailable: false });
    }

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const clinic = this.doctorClinics.find(c => c.clinicId === this.followUpClinicId);
    const openDaysArray = clinic?.openDays
      ? clinic.openDays.split(',').map((d: string) => d.trim())
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
      const isClosedDay = openDaysArray.length > 0 && !openDaysArray.includes(dayName);

      const isAvailable = !isPast && !isClosedDay;
      const isToday = dateObj.getTime() === today.getTime();

      days.push({
        dayNumber: day,
        dateString,
        isAvailable,
        isToday,
        isPast,
        isClosedDay
      });
    }

    this.followUpCalendarDays = days;
  }

  prevFollowUpMonth(): void {
    const m = this.followUpCurrentMonth.getMonth();
    this.followUpCurrentMonth = new Date(this.followUpCurrentMonth.getFullYear(), m - 1, 1);
    this.generateFollowUpCalendar();
  }

  nextFollowUpMonth(): void {
    const m = this.followUpCurrentMonth.getMonth();
    this.followUpCurrentMonth = new Date(this.followUpCurrentMonth.getFullYear(), m + 1, 1);
    this.generateFollowUpCalendar();
  }

  getFollowUpMonthName(): string {
    return this.followUpCurrentMonth.toLocaleString('default', { month: 'long', year: 'numeric' });
  }

  selectFollowUpCalendarDate(day: any): void {
    if (!day.isAvailable) return;
    this.followUpDate = day.dateString;
    this.validateFollowUpDate();
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

  acceptFollowUp(appId: string): void {
    this.appointmentService.acceptFollowUp(appId).subscribe({
      next: () => {
        this.toastService.showSuccess('Follow-up appointment scheduled and confirmed!');
        this.loadDashboardData();
      },
      error: (err) => {
        this.toastService.showError(err, 'Failed to accept follow-up.');
      }
    });
  }

  rejectFollowUp(appId: string): void {
    this.appointmentService.declineFollowUp(appId).subscribe({
      next: () => {
        this.toastService.showSuccess('Follow-up appointment declined.');
        this.loadDashboardData();
      },
      error: (err) => {
        this.toastService.showError(err, 'Failed to decline follow-up.');
      }
    });
  }

  openPatientDetailsModal(patientId: string): void {
    this.selectedPatientDetails = null;
    this.showPatientDetailsModal = true;
    this.isDetailsLoading = true;
    document.body.style.overflow = 'hidden';
    document.documentElement.style.overflow = 'hidden';

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
    document.body.style.overflow = '';
    document.documentElement.style.overflow = '';
  }

  openDoctorDetailsModal(doctorId: string): void {
    this.selectedDoctorDetails = null;
    this.showDoctorDetailsModal = true;
    this.isDoctorDetailsLoading = true;
    document.body.style.overflow = 'hidden';
    document.documentElement.style.overflow = 'hidden';

    this.patientService.getDoctorProfileById(doctorId).subscribe({
      next: (res) => {
        this.selectedDoctorDetails = res;
        this.isDoctorDetailsLoading = false;
      },
      error: (err) => {
        this.toastService.showError(err, 'Failed to fetch doctor details.');
        this.isDoctorDetailsLoading = false;
        this.closeDoctorDetailsModal();
      }
    });
  }

  closeDoctorDetailsModal(): void {
    this.showDoctorDetailsModal = false;
    this.selectedDoctorDetails = null;
    document.body.style.overflow = '';
    document.documentElement.style.overflow = '';
  }



  openHistoryModal(patientId: string, patientName: string): void {
    this.selectedPatientName = patientName;
    this.patientHistory = [];
    this.historyClinicFilters = {};
    this.historyStatusFilters = {
      Completed: true,
      Confirmed: true,
      Pending: true,
      Cancelled: true,
      Rejected: true
    };
    this.showHistoryModal = true;
    this.isHistoryLoading = true;
    document.body.style.overflow = 'hidden';
    document.documentElement.style.overflow = 'hidden';

    this.appointmentService.getAdminDoctorDashboard({ patientId: patientId }, 1, 100).subscribe({
      next: (res) => {
        this.patientHistory = res.items.sort((a, b) => new Date(b.appointmentDate).getTime() - new Date(a.appointmentDate).getTime());
        this.patientHistory.forEach(item => {
          const clinic = item.clinicName || 'Direct';
          this.historyClinicFilters[clinic] = true;
        });
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
    this.historyClinicFilters = {};
    this.historyStatusFilters = {};
    document.body.style.overflow = '';
    document.documentElement.style.overflow = '';
  }

  getUniqueClinicsFromHistory(): string[] {
    const clinics = this.patientHistory
      .map(h => h.clinicName || 'Direct')
      .filter(name => !!name);
    return Array.from(new Set(clinics));
  }

  getSelectedClinicsCount(): number {
    return Object.values(this.historyClinicFilters).filter(v => v).length;
  }

  isAllHistoryClinicsSelected(): boolean {
    const clinics = this.getUniqueClinicsFromHistory();
    if (clinics.length === 0) return false;
    return clinics.every(c => this.historyClinicFilters[c] !== false);
  }

  toggleAllHistoryClinics(checked: boolean): void {
    const clinics = this.getUniqueClinicsFromHistory();
    clinics.forEach(c => this.historyClinicFilters[c] = checked);
  }

  getSelectedStatusesCount(): number {
    return Object.values(this.historyStatusFilters).filter(v => v).length;
  }

  isAllHistoryStatusesSelected(): boolean {
    const statuses = ['Completed', 'Confirmed', 'Pending', 'Cancelled', 'Rejected'];
    return statuses.every(s => this.historyStatusFilters[s] !== false);
  }

  toggleAllHistoryStatuses(checked: boolean): void {
    const statuses = ['Completed', 'Confirmed', 'Pending', 'Cancelled', 'Rejected'];
    statuses.forEach(s => this.historyStatusFilters[s] = checked);
  }

  getFilteredHistory(): Appointment[] {
    return this.patientHistory.filter(h => {
      // Clinic Match
      const clinic = h.clinicName || 'Direct';
      const clinicMatch = this.historyClinicFilters[clinic] !== false;

      // Status Match
      const status = h.status;
      let statusMatch = false;
      if (status === 'Completed') statusMatch = this.historyStatusFilters['Completed'] !== false;
      else if (status === 'Confirmed') statusMatch = this.historyStatusFilters['Confirmed'] !== false;
      else if (status === 'Pending' || status === 'RescheduleProposed' || status === 'FollowUpProposed') {
        statusMatch = this.historyStatusFilters['Pending'] !== false;
      }
      else if (status === 'Cancelled') statusMatch = this.historyStatusFilters['Cancelled'] !== false;
      else if (status === 'Rejected') statusMatch = this.historyStatusFilters['Rejected'] !== false;

      return clinicMatch && statusMatch;
    });
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
      case 'FollowUpProposed': return '#06b6d4';
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
      case 'FollowUpProposed': return 'Follow-up Proposed';
      default: return status;
    }
  }

  getPatientBadgeText(status: string): string {
    switch (status) {
      case 'Completed': return '✓ Checked';
      case 'Pending': return 'Waitlist';
      case 'RescheduleProposed': return '⏳ Rescheduling';
      case 'Cancelled': return '❌ Cancelled';
      case 'Rejected': return '❌ Rejected';
      case 'Skipped': return '⏳ Skip / Late';
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

  showTimelineModal = false;
  isTimelineLoading = false;
  timelineLogs: any[] = [];

  openTimelineModal(appointmentId: string): void {
    this.showTimelineModal = true;
    this.isTimelineLoading = true;
    this.timelineLogs = [];

    this.appointmentService.getAuditLogs(1, 100, undefined, appointmentId).subscribe({
      next: (res) => {
        this.timelineLogs = res.items || [];
        this.isTimelineLoading = false;
      },
      error: (err) => {
        console.error('Failed to load timeline', err);
        this.isTimelineLoading = false;
        this.toastService.showError('Failed to load audit trail.');
      }
    });
  }

  closeTimelineModal(): void {
    this.showTimelineModal = false;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const elements = document.querySelectorAll('details.multiselect-dropdown');
    elements.forEach(el => {
      if (!el.contains(event.target as Node)) {
        el.removeAttribute('open');
      }
    });
  }
}
