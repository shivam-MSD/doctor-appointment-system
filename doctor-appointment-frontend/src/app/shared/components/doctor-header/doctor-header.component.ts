import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-doctor-header',
  templateUrl: './doctor-header.component.html',
  styleUrls: ['./doctor-header.component.css']
})
export class DoctorHeaderComponent {
  @Input() firstName = '';
  @Input() isDoctorAddressIncomplete = false;
  @Input() doctorClinics: any[] = [];
  @Input() isClinicsLoading = false;

  @Output() openClinicModal = new EventEmitter<void>();

  isOnDuty = true;

  onOpenClinicModal() {
    this.openClinicModal.emit();
  }

  toggleOnDuty() {
    this.isOnDuty = !this.isOnDuty;
  }
}
