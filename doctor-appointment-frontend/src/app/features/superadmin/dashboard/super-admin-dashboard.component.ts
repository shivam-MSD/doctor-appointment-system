// now we will start updating the UI for all the devices
import { Component, OnInit, OnDestroy } from '@angular/core';
import { AdminService } from '../../../core/services/admin.service';
import { ToastService } from '../../../core/services/toast.service';
import { NotificationService } from '../../../core/services/notification.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-super-admin-dashboard',
  templateUrl: './super-admin-dashboard.component.html',
  styleUrls: ['./super-admin-dashboard.component.css']
})
export class SuperAdminDashboardComponent implements OnInit, OnDestroy {
  pendingDoctors: any[] = [];
  pendingClinics: any[] = [];
  pendingAdmins: any[] = [];
  errorMessage = '';
  successMessage = '';
  isSuperAdminLoading = true;
  private signalrSub?: Subscription;

  // Reject clinic states
  showRejectModal = false;
  selectedClinicIdForRejection = '';
  rejectionReason = '';

  // Detail Modal States
  showDoctorDetailsModal = false;
  selectedDoctorDetails: any = null;
  isDoctorVerified = false;

  showClinicDetailsModal = false;
  selectedClinicDetails: any = null;
  isClinicVerified = false;

  showAdminDetailsModal = false;
  selectedAdminDetails: any = null;
  isAdminVerified = false;

  // Clinic History State
  showHistoryModal = false;
  clinicHistory: any[] = [];
  selectedClinicNameForHistory = '';

  constructor(
    private adminService: AdminService,
    private toastService: ToastService,
    private notificationService: NotificationService
  ) { }

  ngOnInit(): void {
    this.loadPendingRequests();

    // Auto-reload the dashboard pending queues in real-time when refresh signals are received
    this.signalrSub = this.notificationService.refreshData$.subscribe({
      next: (area) => {
        if (area === 'Clinics' || area === 'Doctors' || area === 'Admins') {
          this.loadPendingRequests();
        }
      }
    });
  }

  ngOnDestroy(): void {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
  }

  loadPendingRequests(): void {
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

  approveDoctor(doctorUserId: string): void {
    this.adminService.approveDoctor(doctorUserId).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message || 'Doctor approved successfully!');
        this.loadPendingRequests();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to approve doctor.');
      }
    });
  }

  verifyClinic(clinicId: string): void {
    this.adminService.verifyClinic(clinicId).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message || 'Clinic verified successfully!');
        this.loadPendingRequests();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to verify clinic.');
      }
    });
  }

  verifyAdmin(adminId: string): void {
    this.adminService.verifyAdmin(adminId).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message || 'Clinic Admin approved successfully!');
        this.loadPendingRequests();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to approve admin.');
      }
    });
  }

  rejectDoctor(doctorUserId: string): void {
    this.adminService.rejectDoctor(doctorUserId).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message || 'Doctor rejected successfully.');
        this.loadPendingRequests();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to reject doctor.');
      }
    });
  }

  rejectAdmin(adminId: string): void {
    this.adminService.rejectAdmin(adminId).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message || 'Clinic Admin rejected successfully.');
        this.loadPendingRequests();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to reject admin.');
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
        this.loadPendingRequests();
      },
      error: (err) => {
        this.toastService.showError(err?.error?.detail || 'Failed to reject clinic.');
      }
    });
  }

  // --- Doctor Details Modal ---
  openDoctorDetails(doctor: any): void {
    this.selectedDoctorDetails = doctor;
    this.isDoctorVerified = false;
    this.showDoctorDetailsModal = true;
  }

  closeDoctorDetails(): void {
    this.showDoctorDetailsModal = false;
    this.selectedDoctorDetails = null;
  }

  // --- Clinic Details Modal ---
  openClinicDetails(clinic: any): void {
    this.selectedClinicDetails = clinic;
    this.isClinicVerified = false;
    this.showClinicDetailsModal = true;
  }

  closeClinicDetails(): void {
    this.showClinicDetailsModal = false;
    this.selectedClinicDetails = null;
  }

  // --- Admin Details Modal ---
  openAdminDetails(admin: any): void {
    this.selectedAdminDetails = admin;
    this.isAdminVerified = false;
    this.showAdminDetailsModal = true;
  }

  closeAdminDetails(): void {
    this.showAdminDetailsModal = false;
    this.selectedAdminDetails = null;
  }

  // --- Clinic History Modal ---
  openClinicHistory(clinic: any): void {
    this.selectedClinicNameForHistory = clinic.clinicName;
    const targetId = clinic.parentClinicId || clinic.clinicId;
    this.adminService.getClinicHistory(targetId).subscribe({
      next: (history) => {
        this.clinicHistory = history;
        this.showHistoryModal = true;
      },
      error: () => {
        this.toastService.showError('Failed to load clinic history.');
      }
    });
  }

  closeHistoryModal(): void {
    this.showHistoryModal = false;
    this.clinicHistory = [];
    this.selectedClinicNameForHistory = '';
  }

  // Helper method for safely parsing JSON in HTML template
  parseJson(jsonString: string): any {
    try {
      return jsonString ? JSON.parse(jsonString) : null;
    } catch {
      return null;
    }
  }

  parseChanges(log: any): any[] {
    try {
      const oldObj = JSON.parse(log.oldDataJson || '{}');
      const newObj = JSON.parse(log.newDataJson || '{}');
      const changes: any[] = [];

      const fieldLabels: { [key: string]: string } = {
        ClinicName: 'Clinic Name',
        ClinicType: 'Clinic Type',
        ContactNumber: 'Contact Number',
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
        MaxAppointmentsPerDay: 'Max Appointments/Day',
        State: 'State',
        City: 'City',
        Pincode: 'Pincode',
        Area: 'Area',
        Addressline1: 'Address Line 1',
        Addressline2: 'Address Line 2'
      };

      const keys = Object.keys(fieldLabels);
      for (const key of keys) {
        const camelKey = key.charAt(0).toLowerCase() + key.slice(1);
        let oldVal = oldObj[key] !== undefined ? oldObj[key] : oldObj[camelKey];
        let newVal = newObj[key] !== undefined ? newObj[key] : newObj[camelKey];

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
