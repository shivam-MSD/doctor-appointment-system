import { Component, OnInit, OnDestroy } from '@angular/core';
import { AppointmentService } from '../../../core/services/appointment.service';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';
import { Appointment } from '../../../core/models/appointment.model';
import { Subscription } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-doctor-appointments',
  templateUrl: './doctor-appointments.component.html',
  styleUrls: ['./doctor-appointments.component.css']
})
export class DoctorAppointmentsComponent implements OnInit, OnDestroy {
  appointments: Appointment[] = [];
  doctorClinics: any[] = [];

  // Filters
  selectedClinicId = '';
  searchQuery = '';
  statusFilter = '';
  consultationFilter = '';
  dateFilter = '';
  
  // Tabs: 'upcoming', 'past', 'cancelled', 'all'
  activeTab: 'upcoming' | 'past' | 'cancelled' | 'all' = 'upcoming';

  // Pagination
  page = 1;
  size = 10;
  role = '';

  isLoading = false;
  errorMessage = '';
  private signalrSub?: Subscription;

  // Patient Details Modal States
  showPatientDetailsModal = false;
  selectedPatientDetails: any = null;
  isDetailsLoading = false;

  // Reschedule Propose Modal State
  showRescheduleModal = false;
  selectedRescheduleAppId = '';
  rescheduleDate = '';
  rescheduleTime = '';
  rescheduleReason = '';

  // Assign Time Modal State
  showAssignTimeModal = false;
  selectedAssignTimeAppId = '';
  selectedAssignTimeAppDate = '';
  assignTimeInput = '';
  assignTimeComment = '';

  constructor(
    private appointmentService: AppointmentService,
    private authService: AuthService,
    private toastService: ToastService,
    private notificationService: NotificationService
  ) { }

  ngOnInit(): void {
    this.role = this.authService.getRole() || '';
    if (this.role === 'Doctor') {
      this.loadDoctorClinics();
    }
    this.loadAppointments();

    this.signalrSub = this.notificationService.refreshData$.subscribe({
      next: (hubName) => {
        if (hubName === 'Appointments') {
          this.loadAppointments();
        }
      }
    });
  }

  loadDoctorClinics(): void {
    const profileId = sessionStorage.getItem('profileId');
    if (!profileId) return;
    this.appointmentService.getClinicsForDoctor(profileId).subscribe({
      next: (res) => {
        this.doctorClinics = res;
      }
    });
  }

  loadAppointments(): void {
    this.isLoading = true;
    this.errorMessage = '';

    const filters: any = {};
    if (this.statusFilter) filters.status = this.statusFilter;
    if (this.searchQuery) filters.search = this.searchQuery;

    this.appointmentService.getAdminDoctorDashboard(filters, 1, 1000).subscribe({
      next: (res) => {
        this.appointments = res.items;
        this.isLoading = false;
      },
      error: (err) => {
        this.toastService.showError(err, 'Failed to retrieve appointments record.');
        this.isLoading = false;
      }
    });
  }

  getFilteredAppointments(): Appointment[] {
    let list = this.appointments || [];
    if (this.selectedClinicId) {
      list = list.filter(app => app.clinicId === this.selectedClinicId);
    }
    if (this.statusFilter) {
      list = list.filter(app => app.status === this.statusFilter);
    }
    if (this.consultationFilter) {
      list = list.filter(app => app.consultationType === this.consultationFilter);
    }
    if (this.dateFilter) {
      list = list.filter(app => app.appointmentDate.startsWith(this.dateFilter));
    }

    // Apply Tab Filtering
    const today = new Date();
    today.setHours(0, 0, 0, 0);

    if (this.activeTab === 'upcoming') {
      list = list.filter(app => {
        if (app.status === 'Pending' || app.status === 'Confirmed' || app.status === 'RescheduleProposed') {
          const appDate = new Date(app.appointmentDate);
          appDate.setHours(0, 0, 0, 0);
          return appDate >= today;
        }
        return false;
      });
    } else if (this.activeTab === 'past') {
      list = list.filter(app => {
        if (app.status === 'Completed') return true;
        if (app.status === 'Pending' || app.status === 'Confirmed') {
          const appDate = new Date(app.appointmentDate);
          appDate.setHours(0, 0, 0, 0);
          return appDate < today;
        }
        return false;
      });
    } else if (this.activeTab === 'cancelled') {
      list = list.filter(app => app.status === 'Cancelled' || app.status === 'Rejected');
    }
    // 'all' tab does no extra filtering

    return list;
  }

  get totalCount(): number {
    return this.getFilteredAppointments().length;
  }

  getPaginatedAppointments(): Appointment[] {
    const list = this.getFilteredAppointments();
    const startIndex = (this.page - 1) * this.size;
    return list.slice(startIndex, startIndex + this.size);
  }

  setTab(tab: string): void {
    this.activeTab = tab as any;
    this.page = 1;
    this.statusFilter = ''; // Reset status filter when switching tabs
  }

  getStatusColor(status: string): string {
    switch (status) {
      case 'Confirmed': return '#10b981';
      case 'Pending': return '#f59e0b';
      case 'Cancelled': return '#ef4444';
      case 'Rejected': return '#dc2626';
      case 'RescheduleProposed': return '#ec4899';
      case 'Completed': return '#8b5cf6';
      default: return '#8b5cf6';
    }
  }

  selectClinic(clinicId: string): void {
    this.selectedClinicId = clinicId;
    this.onFilterChange();
  }

  onFilterChange(): void {
    this.page = 1;
    this.loadAppointments();
  }

  resetFilters(): void {
    this.searchQuery = '';
    this.statusFilter = '';
    this.consultationFilter = '';
    this.dateFilter = '';
    this.activeTab = 'all';
    this.selectedClinicId = '';
    this.page = 1;
    this.loadAppointments();
  }

  prevPage(): void {
    if (this.page > 1) {
      this.page--;
    }
  }

  nextPage(): void {
    if (this.page * this.size < this.totalCount) {
      this.page++;
    }
  }

  totalPages(): number {
    return Math.ceil(this.totalCount / this.size) || 1;
  }

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'confirmed': return 'badge badge-confirmed';
      case 'pending': return 'badge badge-pending';
      case 'completed': return 'badge badge-confirmed'; // Green
      case 'cancelled': return 'badge badge-cancelled';
      case 'rejected': return 'badge badge-cancelled';
      default: return 'badge';
    }
  }

  cancelAppointment(appId: string): void {
    if (confirm('Are you sure you want to cancel this appointment?')) {
      const reason = prompt('Please enter a reason for cancellation (optional):') || 'Cancelled by doctor/admin.';
      this.appointmentService.doctorCancelAppointment(appId, reason).subscribe({
        next: () => {
          this.toastService.showSuccess('Appointment cancelled successfully.');
          this.loadAppointments();
        },
        error: (err: any) => {
          this.toastService.showError(err, 'Failed to cancel appointment.');
        }
      });
    }
  }

  // Reschedule Propose Methods
  openRescheduleModal(appId: string): void {
    this.selectedRescheduleAppId = appId;
    this.rescheduleDate = '';
    this.rescheduleTime = '';
    this.rescheduleTime = '';
    this.rescheduleReason = '';
    this.showRescheduleModal = true;
  }

  validateRescheduleDate(): void {
    if (!this.rescheduleDate || !this.selectedRescheduleAppId) return;
    
    const app = this.appointments.find(a => a.appointmentId === this.selectedRescheduleAppId);
    if (!app) return;

    const clinic = this.doctorClinics.find(c => c.clinicId === app.clinicId);
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
        this.loadAppointments();
      },
      error: (err: any) => {
        this.toastService.showError(err, 'Failed to propose reschedule.');
      }
    });
  }

  // Assign Time Methods
  openAssignTimeModal(appId: string, appDate: string): void {
    this.selectedAssignTimeAppId = appId;
    this.selectedAssignTimeAppDate = new Date(appDate).toISOString().split('T')[0]; // Store as YYYY-MM-DD
    this.assignTimeInput = '';
    this.assignTimeComment = '';
    this.showAssignTimeModal = true;
  }

  closeAssignTimeModal(): void {
    this.showAssignTimeModal = false;
    this.selectedAssignTimeAppId = '';
    this.selectedAssignTimeAppDate = '';
  }

  submitAssignTime(): void {
    if (!this.assignTimeInput) {
      this.toastService.showError('Please select a time.');
      return;
    }

    const formattedTime = `${this.selectedAssignTimeAppDate}T${this.assignTimeInput}:00`;
    
    this.appointmentService.assignAppointmentTime(this.selectedAssignTimeAppId, formattedTime, this.assignTimeComment).subscribe({
      next: () => {
        this.toastService.showSuccess('Time assigned successfully.');
        this.closeAssignTimeModal();
        this.loadAppointments();
      },
      error: (err: any) => {
        this.toastService.showError(err, 'Failed to assign time.');
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

  ngOnDestroy(): void {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
  }


  getStatusTitle(status: string): string {
    switch (status) {
      case 'Confirmed': return 'Confirmed';
      case 'Pending': return 'Confirmation Pending';
      case 'Cancelled': return 'Cancelled';
      case 'Rejected': return 'Rejected';
      case 'Completed': return 'Completed';
      default: return status;
    }
  }

  getConsultationTypeLabel(type: string): string {
    return type === 'InPerson' ? '🏠 In-Person Visit' : '🎥 Video Consultation';
  }
}
