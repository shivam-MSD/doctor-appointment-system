import { Component, Input, Output, EventEmitter } from '@angular/core';

@Component({
  selector: 'app-admin-header',
  templateUrl: './admin-header.component.html',
  styleUrls: ['./admin-header.component.css']
})
export class AdminHeaderComponent {
  @Input() firstName = '';
  @Input() adminClinic: any = null;

  @Output() openAdminEditClinicModal = new EventEmitter<void>();

  onOpenAdminEditClinicModal() {
    this.openAdminEditClinicModal.emit();
  }

  isClinicCurrentlyOpen(clinic: any): boolean {
    if (!clinic || !clinic.isAvailable) return false;
    
    // Day check
    if (clinic.openDays) {
      const days = clinic.openDays.split(',').map((d: string) => d.trim());
      const today = new Date().toLocaleString('en-US', { weekday: 'short' });
      const fullToday = new Date().toLocaleString('en-US', { weekday: 'long' });
      if (!days.includes(today) && !days.includes(fullToday)) {
        return false;
      }
    }

    // Time check
    if (clinic.startTime && clinic.endTime) {
      const now = new Date();
      const currentHours = now.getHours();
      const currentMinutes = now.getMinutes();
      const currentTime = currentHours + (currentMinutes / 60);

      const parseTime = (tStr: string) => {
        const [time, modifier] = tStr.split(' ');
        let [hours, minutes] = time.split(':').map(Number);
        if (modifier === 'PM' && hours < 12) hours += 12;
        if (modifier === 'AM' && hours === 12) hours = 0;
        return hours + ((minutes || 0) / 60);
      };

      const start = parseTime(clinic.startTime);
      const end = parseTime(clinic.endTime);

      if (currentTime < start || currentTime > end) {
        return false;
      }
    }

    return true;
  }
}
