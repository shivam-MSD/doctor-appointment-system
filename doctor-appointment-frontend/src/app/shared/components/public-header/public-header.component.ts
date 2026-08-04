import { Component, OnInit, Output, EventEmitter, Input } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterModule } from '@angular/router';
import { PatientService } from '../../../core/services/patient.service';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-public-header',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterModule],
  templateUrl: './public-header.component.html',
  styleUrls: ['./public-header.component.css']
})
export class PublicHeaderComponent implements OnInit {
  @Input() selectedCity = '';
  @Input() searchQuery = '';

  @Output() cityChange = new EventEmitter<string>();
  @Output() searchChange = new EventEmitter<string>();

  cities: any[] = [];
  isLoggedIn = false;
  currentUser: any = null;

  constructor(
    private patientService: PatientService,
    public authService: AuthService,
    private router: Router
  ) {}

  ngOnInit(): void {
    this.isLoggedIn = this.authService.isLoggedIn();
    this.currentUser = this.authService.getAnyActiveUser();

    this.patientService.getTopCities().subscribe({
      next: (data) => {
        this.cities = data;
      },
      error: () => {
        this.cities = [
          { Name: 'All Cities', Code: '' },
          { Name: 'Mumbai', Code: 'Mumbai' },
          { Name: 'Delhi', Code: 'Delhi' },
          { Name: 'Bangalore', Code: 'Bangalore' },
          { Name: 'Ahmedabad', Code: 'Ahmedabad' },
          { Name: 'Pune', Code: 'Pune' }
        ];
      }
    });
  }

  onCitySelect(cityCode: string): void {
    this.selectedCity = cityCode;
    this.cityChange.emit(cityCode);
  }

  onSearchInput(): void {
    this.searchChange.emit(this.searchQuery);
  }

  onLogout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
