import { Component, OnInit, OnDestroy } from '@angular/core';
import { AdminService } from '../../../core/services/admin.service';
import { ToastService } from '../../../core/services/toast.service';
import { NotificationService } from '../../../core/services/notification.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-super-admin-clinics',
  templateUrl: './super-admin-clinics.component.html',
  styleUrls: ['./super-admin-clinics.component.css']
})
export class SuperAdminClinicsComponent implements OnInit, OnDestroy {
  clinics: any[] = [];
  searchQuery = '';
  stateFilter = '';
  cityFilter = '';
  verifiedFilter: boolean | undefined = undefined;
  errorMessage = '';
  successMessage = '';
  isClinicsLoading = true;
  private signalrSub?: Subscription;

  // Reject clinic states
  showRejectModal = false;
  selectedClinicIdForRejection = '';
  rejectionReason = '';

  // View Details modal states
  showDetailsModal = false;
  selectedClinicForDetails: any = null;
  isDetailsVerified = false;

  // History Tracking states
  showHistoryModal = false;
  selectedClinicHistory: any[] = [];
  selectedClinicNameForHistory = '';

  constructor(
    private adminService: AdminService,
    private toastService: ToastService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadClinics();

    // Auto-reload the list in real-time when silent refresh signals are received
    this.signalrSub = this.notificationService.refreshData$.subscribe({
      next: (area) => {
        if (area === 'Clinics') {
          this.loadClinics();
        }
      }
    });
  }

  ngOnDestroy(): void {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
  }

  loadClinics(): void {
    this.isClinicsLoading = true;
    this.adminService.getAllClinics(this.searchQuery, this.stateFilter, this.cityFilter, this.verifiedFilter).subscribe({
      next: (res) => {
        this.clinics = res;
        this.isClinicsLoading = false;
      },
      error: () => {
        this.toastService.showError('Failed to load clinics list.');
        this.isClinicsLoading = false;
      }
    });
  }

  verifyClinic(clinicId: string): void {
    this.adminService.verifyClinic(clinicId).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message || 'Clinic branch verified successfully!');
        this.loadClinics();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to verify clinic branch.');
      }
    });
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
        this.loadClinics();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to reject clinic.');
      }
    });
  }

  // View Details methods
  openDetailsModal(clinic: any): void {
    this.selectedClinicForDetails = clinic;
    this.isDetailsVerified = false;
    this.showDetailsModal = true;
  }

  closeDetailsModal(): void {
    this.showDetailsModal = false;
    this.selectedClinicForDetails = null;
    this.isDetailsVerified = false;
  }

  // History methods
  viewHistory(clinicId: string, clinicName: string): void {
    this.selectedClinicNameForHistory = clinicName;
    this.adminService.getClinicHistory(clinicId).subscribe({
      next: (res) => {
        this.selectedClinicHistory = res;
        this.showHistoryModal = true;
      },
      error: () => {
        this.toastService.showError('Failed to load clinic audit history.');
      }
    });
  }

  closeHistoryModal(): void {
    this.showHistoryModal = false;
    this.selectedClinicHistory = [];
    this.selectedClinicNameForHistory = '';
  }

  parseChanges(log: any): any[] {
    try {
      const oldObj = JSON.parse(log.oldDataJson || '{}');
      const newObj = JSON.parse(log.newDataJson || '{}');
      const changes: any[] = [];

      const fieldLabels: { [key: string]: string } = {
        ClinicName: 'Clinic Name',
        ClinicType: 'Clinic Type',
        OpenDays: 'Open Days',
        StartTime: 'Opening Time',
        EndTime: 'Closing Time',
        IsAvailable: 'Branch Active/Open',
        UnavailabilityReason: 'Branch Closed Reason',
        IsDoctorAvailable: 'Doctor Available Personally',
        DoctorUnavailabilityReason: 'Unavailability Reason',
        BookingWindowEndDate: 'Booking End Date',
        BookingWindowStartDate: 'Booking Start Date',
        SupportedModes: 'Supported Modes',
        State: 'State',
        City: 'City',
        Pincode: 'Pincode',
        Area: 'Area',
        Addressline1: 'Address Line 1',
        Addressline2: 'Address Line 2'
      };

      const keys = Object.keys(fieldLabels);
      for (const key of keys) {
        let oldVal = oldObj[key];
        let newVal = newObj[key];

        if (key === 'BookingWindowEndDate' || key === 'BookingWindowStartDate') {
          if (oldVal) oldVal = new Date(oldVal).toLocaleDateString();
          if (newVal) newVal = new Date(newVal).toLocaleDateString();
        }

        if (typeof oldVal === 'boolean') oldVal = oldVal ? 'Yes' : 'No';
        if (typeof newVal === 'boolean') newVal = newVal ? 'Yes' : 'No';

        const normalize = (val: any) => (val === null || val === undefined) ? '' : String(val).trim();
        if (normalize(oldVal) !== normalize(newVal)) {
          changes.push({
            label: fieldLabels[key],
            oldVal: normalize(oldVal) || 'N/A',
            newVal: normalize(newVal) || 'N/A'
          });
        }
      }

      return changes;
    } catch (e) {
      return [];
    }
  }
}
