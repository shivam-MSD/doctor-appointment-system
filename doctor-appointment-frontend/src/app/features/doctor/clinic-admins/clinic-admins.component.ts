import { Component, OnInit, OnDestroy } from '@angular/core';
import { AdminService } from '../../../core/services/admin.service';
import { NotificationService } from '../../../core/services/notification.service';
import { Subscription } from 'rxjs';

@Component({
  selector: 'app-clinic-admins',
  templateUrl: './clinic-admins.component.html',
  styleUrls: ['./clinic-admins.component.css']
})
export class ClinicAdminsComponent implements OnInit, OnDestroy {
  clinicAdmins: any[] = [];
  errorMessage = '';
  private signalrSub?: Subscription;

  constructor(
    private adminService: AdminService,
    private notificationService: NotificationService
  ) {}

  ngOnInit(): void {
    this.loadClinicAdmins();

    // Auto-reload the clinic admin roster in real-time when refresh signals are received
    this.signalrSub = this.notificationService.refreshData$.subscribe({
      next: (area) => {
        if (area === 'Admins') {
          this.loadClinicAdmins();
        }
      }
    });
  }

  ngOnDestroy(): void {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
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
