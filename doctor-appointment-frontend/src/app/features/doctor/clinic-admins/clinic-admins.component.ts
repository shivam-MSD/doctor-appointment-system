import { Component, OnInit } from '@angular/core';
import { AdminService } from '../../../core/services/admin.service';

@Component({
  selector: 'app-clinic-admins',
  templateUrl: './clinic-admins.component.html',
  styleUrls: ['./clinic-admins.component.css']
})
export class ClinicAdminsComponent implements OnInit {
  clinicAdmins: any[] = [];
  errorMessage = '';

  constructor(private adminService: AdminService) {}

  ngOnInit(): void {
    this.loadClinicAdmins();
  }

  loadClinicAdmins(): void {
    this.adminService.getDoctorAdmins().subscribe({
      next: (res) => {
        this.clinicAdmins = res;
      },
      error: () => {
        this.errorMessage = 'Failed to load clinic administrators.';
      }
    });
  }
}
