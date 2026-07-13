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
  statusFilter = '';
  searchQuery = '';

  // Pagination
  page = 1;
  size = 10;
  totalCount = 0;

  isLoading = false;
  errorMessage = '';
  private signalrSub?: Subscription;

  // Patient Details Modal States
  showPatientDetailsModal = false;
  selectedPatientDetails: any = null;
  isDetailsLoading = false;

  constructor(
    private appointmentService: AppointmentService,
    private authService: AuthService,
    private toastService: ToastService,
    private notificationService: NotificationService
  ) { }

  ngOnInit(): void {
    this.loadDoctorClinics();
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

    this.appointmentService.getAdminDoctorDashboard(filters, this.page, this.size).subscribe({
      next: (res) => {
        this.appointments = res.items;
        this.totalCount = res.totalCount;
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
    if (!this.statusFilter) {
      list = list.filter(app => app.status !== 'Pending');
    }
    return list;
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
    this.selectedClinicId = '';
    this.statusFilter = '';
    this.searchQuery = '';
    this.page = 1;
    this.loadAppointments();
  }

  prevPage(): void {
    if (this.page > 1) {
      this.page--;
      this.loadAppointments();
    }
  }

  nextPage(): void {
    if (this.page * this.size < this.totalCount) {
      this.page++;
      this.loadAppointments();
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

  getStatusColor(status: string): string {
    switch (status) {
      case 'Confirmed': return '#10b981';
      case 'Pending': return '#f59e0b';
      case 'Cancelled': return '#ef4444';
      case 'Rejected': return '#dc2626';
      case 'Completed': return '#8b5cf6';
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
      default: return status;
    }
  }

  getConsultationTypeLabel(type: string): string {
    return type === 'InPerson' ? '🏠 In-Person Visit' : '🎥 Video Consultation';
  }
}
