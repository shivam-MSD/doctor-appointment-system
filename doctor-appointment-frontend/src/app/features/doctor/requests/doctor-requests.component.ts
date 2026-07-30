import { Component, OnInit, OnDestroy, HostListener } from '@angular/core';
import { AppointmentService } from '../../../core/services/appointment.service';
import { ToastService } from '../../../core/services/toast.service';
import { Appointment } from '../../../core/models/appointment.model';
import { Subscription } from 'rxjs';
import { NotificationService } from '../../../core/services/notification.service';

@Component({
  selector: 'app-doctor-requests',
  templateUrl: './doctor-requests.component.html',
  styleUrls: ['./doctor-requests.component.css']
})
export class DoctorRequestsComponent implements OnInit, OnDestroy {
  pendingRequests: Appointment[] = [];
  dateFilter = '';
  consultationFilter = '';
  isLoading = false;
  errorMessage = '';
  selectedClinicId = '';
  doctorClinics: any[] = [];
  private signalrSub?: Subscription;

  // Approve Modal States
  showApproveModal = false;
  selectedApproveId = '';
  selectedApproveAppDate = '';
  approveComment = '';
  approveTimeInput = '';

  // Reject Modal States
  showRejectModal = false;
  selectedRejectId = '';
  rejectReason = '';

  // Patient Details Modal States
  showPatientDetailsModal = false;
  selectedPatientDetails: any = null;
  isDetailsLoading = false;

  // History Modal States
  showHistoryModal = false;
  selectedPatientName = '';
  patientHistory: Appointment[] = [];
  isHistoryLoading = false;
  historyClinicFilters: { [clinicName: string]: boolean } = {};
  historyStatusFilters: { [statusName: string]: boolean } = {};

  constructor(
    private appointmentService: AppointmentService,
    private toastService: ToastService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadDoctorClinics();
    this.loadPendingRequests();

    this.signalrSub = this.notificationService.refreshData$.subscribe({
      next: (hubName) => {
        if (hubName === 'Appointments') {
          this.loadPendingRequests();
        }
      }
    });
  }

  loadPendingRequests(): void {
    this.isLoading = true;
    this.errorMessage = '';

    // Fetch all pending requests for Doctor (high limit to list all requests)
    this.appointmentService.getAdminDoctorDashboard({ status: 'Pending' }, 1, 100).subscribe({
      next: (res) => {
        this.pendingRequests = res.items;
        this.isLoading = false;
      },
      error: (err) => {
        this.toastService.showError(err, 'Failed to retrieve appointment requests.');
        this.isLoading = false;
      }
    });
  }

  // Approve Logic
  openApproveModal(id: string, date: string): void {
    this.selectedApproveId = id;
    this.selectedApproveAppDate = new Date(date).toISOString().split('T')[0];
    this.approveComment = '';
    this.approveTimeInput = '';
    this.showApproveModal = true;
  }

  closeApproveModal(): void {
    this.showApproveModal = false;
    this.selectedApproveId = '';
    this.selectedApproveAppDate = '';
    this.approveComment = '';
    this.approveTimeInput = '';
  }

  confirmApprove(): void {
    if (!this.selectedApproveId) return;

    let assignedTime: string | undefined = undefined;
    if (this.approveTimeInput) {
      assignedTime = `${this.selectedApproveAppDate}T${this.approveTimeInput}:00`;
    }

    this.appointmentService.approveAppointment(this.selectedApproveId, this.approveComment, assignedTime).subscribe({
      next: () => {
        this.toastService.showSuccess('Appointment has been approved and confirmed.');
        this.closeApproveModal();
        this.loadPendingRequests();
      },
      error: (err) => {
        this.toastService.showError(err, 'Failed to approve appointment.');
      }
    });
  }

  // Reject Logic
  openRejectModal(id: string): void {
    this.selectedRejectId = id;
    this.rejectReason = '';
    this.showRejectModal = true;
  }

  closeRejectModal(): void {
    this.showRejectModal = false;
    this.selectedRejectId = '';
    this.rejectReason = '';
  }

  confirmReject(): void {
    if (!this.selectedRejectId) return;
    if (!this.rejectReason.trim()) {
      this.toastService.showError('Rejection reason is required.');
      return;
    }

    this.appointmentService.rejectAppointment(this.selectedRejectId, this.rejectReason).subscribe({
      next: () => {
        this.toastService.showSuccess('Appointment request has been rejected.');
        this.closeRejectModal();
        this.loadPendingRequests();
      },
      error: (err) => {
        this.toastService.showError(err, 'Failed to reject appointment.');
      }
    });
  }

  // Patient History Lookup Logic
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

    // Call dashboard endpoint filtering by patient ID to look up previous/upcoming slots
    this.appointmentService.getAdminDoctorDashboard({ patientId: patientId }, 1, 100).subscribe({
      next: (res) => {
        // Sort history by date descending
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

  getStatusClass(status: string): string {
    switch (status?.toLowerCase()) {
      case 'confirmed': return 'badge badge-confirmed';
      case 'pending': return 'badge badge-pending';
      case 'completed': return 'badge badge-confirmed';
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



  loadDoctorClinics(): void {
    const profileId = sessionStorage.getItem('profileId');
    if (!profileId) return;
    this.appointmentService.getClinicsForDoctor(profileId).subscribe({
      next: (res) => {
        this.doctorClinics = res;
      }
    });
  }

  selectClinic(clinicId: string): void {
    this.selectedClinicId = clinicId;
  }

  getFilteredRequests(): Appointment[] {
    let list = this.pendingRequests;
    if (this.selectedClinicId) {
      list = list.filter(app => app.clinicId === this.selectedClinicId);
    }
    if (this.dateFilter) {
      list = list.filter(app => app.appointmentDate.startsWith(this.dateFilter));
    }
    if (this.consultationFilter) {
      list = list.filter(app => app.consultationType === this.consultationFilter);
    }
    return list;
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
