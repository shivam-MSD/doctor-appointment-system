import { Component } from '@angular/core';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.css']
})
export class SidebarComponent {
  constructor(public authService: AuthService) {}

  getCompletionPercentage(): number {
    const stored = sessionStorage.getItem('profileCompletion');
    if (stored) {
      return parseInt(stored, 10);
    }
    return 30; // sensible initial default
  }

  isProfileIncomplete(): boolean {
    const role = this.authService.getRole();
    if (role !== 'Patient' && role !== 'Doctor') return false;
    return this.getCompletionPercentage() < 100;
  }
}
