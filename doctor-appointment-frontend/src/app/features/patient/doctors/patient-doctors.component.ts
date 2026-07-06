import { Component, OnInit } from '@angular/core';
import { PatientService } from '../../../core/services/patient.service';
import { AppointmentService } from '../../../core/services/appointment.service';

@Component({
  selector: 'app-patient-doctors',
  templateUrl: './patient-doctors.component.html',
  styleUrls: ['./patient-doctors.component.css']
})
export class PatientDoctorsComponent implements OnInit {
  doctors: any[] = [];
  specializations: any[] = [];
  
  // Search & Filter Parameters
  searchQuery = '';
  selectedSpecializationId = '';
  stateFilter = '';
  cityFilter = '';

  // Pagination parameters
  page = 1;
  size = 5;
  totalCount = 0;
  totalPages = 1;

  errorMessage = '';

  // Tracks which doctor card is expanded for biography
  expandedDoctorIds: { [id: string]: boolean } = {};

  constructor(
    private patientService: PatientService,
    private appointmentService: AppointmentService
  ) {}

  toggleExpand(doctorId: string): void {
    this.expandedDoctorIds[doctorId] = !this.expandedDoctorIds[doctorId];
  }

  isExpanded(doctorId: string): boolean {
    return !!this.expandedDoctorIds[doctorId];
  }

  ngOnInit(): void {
    this.loadSpecializations();
    this.loadDoctors();
  }

  loadSpecializations(): void {
    this.appointmentService.getSpecializations().subscribe({
      next: (res) => {
        this.specializations = res;
      },
      error: () => {
        this.errorMessage = 'Failed to load doctor specializations.';
      }
    });
  }

  loadDoctors(): void {
    this.errorMessage = '';
    const params: any = {
      page: this.page,
      size: this.size
    };

    if (this.searchQuery.trim()) {
      params.search = this.searchQuery.trim();
    }
    if (this.selectedSpecializationId) {
      params.specializationId = this.selectedSpecializationId;
    }
    if (this.stateFilter.trim()) {
      params.state = this.stateFilter.trim();
    }
    if (this.cityFilter.trim()) {
      params.city = this.cityFilter.trim();
    }

    this.patientService.getDoctorsDirectory(params).subscribe({
      next: (res) => {
        this.doctors = res.items;
        this.totalCount = res.totalCount;
        this.totalPages = res.totalPages;
      },
      error: () => {
        this.errorMessage = 'Failed to load doctors list. Please try again.';
      }
    });
  }

  onFilterChange(): void {
    this.page = 1;
    this.loadDoctors();
  }

  clearFilters(): void {
    this.searchQuery = '';
    this.selectedSpecializationId = '';
    this.stateFilter = '';
    this.cityFilter = '';
    this.page = 1;
    this.loadDoctors();
  }

  onPageChange(newPage: number): void {
    if (newPage >= 1 && newPage <= this.totalPages) {
      this.page = newPage;
      this.loadDoctors();
    }
  }
}
