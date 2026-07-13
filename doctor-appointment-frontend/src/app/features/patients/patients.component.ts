import { Component, OnInit } from '@angular/core';
import { AppointmentService } from '../../core/services/appointment.service';
import { Patient } from '../../core/models/patient.model';
import { Appointment } from '../../core/models/appointment.model';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-patients',
  templateUrl: './patients.component.html',
  styleUrls: ['./patients.component.css']
})
export class PatientsComponent implements OnInit {
  patients: Patient[] = [];
  searchQuery = '';
  genderFilter = '';
  bloodGroupFilter = '';
  ageGroupFilter = '';

  errorMessage = '';
  isLoading = false;

  // View Details Modal
  showDetailsModal = false;
  selectedPatientDetails: Patient | null = null;

  // View Appointment History Modal
  showHistoryModal = false;
  selectedPatientName = '';
  patientHistory: Appointment[] = [];
  isHistoryLoading = false;

  constructor(
    private appointmentService: AppointmentService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.loadPatients();
  }

  loadPatients(): void {
    this.isLoading = true;
    this.errorMessage = '';
    // Load list with high limit to allow local demographic filtering and layout search
    this.appointmentService.getPatientsList(undefined, 1, 100).subscribe({
      next: (res) => {
        // Sort patients by first and last name alphabetically
        this.patients = res.items.sort((a, b) => {
          const nameA = `${a.firstName || ''} ${a.lastName || ''}`.trim().toLowerCase();
          const nameB = `${b.firstName || ''} ${b.lastName || ''}`.trim().toLowerCase();
          return nameA.localeCompare(nameB);
        });
        this.isLoading = false;
      },
      error: () => {
        this.errorMessage = 'Failed to load patients directory.';
        this.isLoading = false;
      }
    });
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

  getFilteredPatients(): Patient[] {
    return this.patients.filter(p => {
      // 1. Keyword search filter (name, phone)
      if (this.searchQuery) {
        const query = this.searchQuery.toLowerCase();
        const fullName = `${p.firstName || ''} ${p.lastName || ''}`.toLowerCase();
        const mobile = (p.mobileNo || '').toLowerCase();
        if (!fullName.includes(query) && !mobile.includes(query)) {
          return false;
        }
      }

      // 2. Gender filter
      if (this.genderFilter && p.gender !== this.genderFilter) {
        return false;
      }

      // 3. Blood group filter
      if (this.bloodGroupFilter && p.bloodGroup !== this.bloodGroupFilter) {
        return false;
      }

      // 4. Age group filter
      if (this.ageGroupFilter) {
        const age = this.getAge(p.dob);
        if (this.ageGroupFilter === 'under18' && age >= 18) return false;
        if (this.ageGroupFilter === '18to35' && (age < 18 || age > 35)) return false;
        if (this.ageGroupFilter === '36to60' && (age < 36 || age > 60)) return false;
        if (this.ageGroupFilter === 'over60' && age <= 60) return false;
      }

      return true;
    });
  }

  resetFilters(): void {
    this.searchQuery = '';
    this.genderFilter = '';
    this.bloodGroupFilter = '';
    this.ageGroupFilter = '';
  }

  // Modal Actions
  openDetailsModal(patient: Patient): void {
    this.selectedPatientDetails = patient;
    this.showDetailsModal = true;
  }

  closeDetailsModal(): void {
    this.showDetailsModal = false;
    this.selectedPatientDetails = null;
  }

  openHistoryModal(patient: Patient): void {
    const fullName = `${patient.firstName} ${patient.lastName}`;
    this.selectedPatientName = fullName;
    this.patientHistory = [];
    this.showHistoryModal = true;
    this.isHistoryLoading = true;

    this.appointmentService.getAdminDoctorDashboard({ patientId: patient.patientId }, 1, 100).subscribe({
      next: (res) => {
        // Sort history by date descending
        this.patientHistory = res.items.sort((a, b) => new Date(b.appointmentDate).getTime() - new Date(a.appointmentDate).getTime());
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
}
