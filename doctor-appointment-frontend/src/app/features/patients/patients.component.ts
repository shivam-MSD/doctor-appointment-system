import { Component, OnInit } from '@angular/core';
import { AppointmentService } from '../../core/services/appointment.service';
import { Patient } from '../../core/models/patient.model';

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

  constructor(private appointmentService: AppointmentService) {}

  ngOnInit(): void {
    this.loadPatients();
  }

  loadPatients(): void {
    this.isLoading = true;
    this.errorMessage = '';
    // Load list with high limit to allow local demographic filtering and layout search
    this.appointmentService.getPatientsList(undefined, 1, 100).subscribe({
      next: (res) => {
        this.patients = res.items;
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
}
