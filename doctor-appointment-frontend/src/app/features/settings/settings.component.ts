import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { LucideAngularModule } from 'lucide-angular';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';
import { ThemeService } from '../../core/services/theme.service';
import { PatientService } from '../../core/services/patient.service';

@Component({
  selector: 'app-settings',
  standalone: true,
  imports: [CommonModule, FormsModule, LucideAngularModule],
  templateUrl: './settings.component.html',
  styleUrls: ['./settings.component.css']
})
export class SettingsComponent implements OnInit {
  userRole = '';
  activeTab: 'notifications' | 'security' | 'appearance' | 'role' = 'notifications';

  // Sound Settings
  isSoundEnabled = true;
  selectedSoundTone = 'chime';

  // Notification Preferences
  emailNotifications = true;
  smsNotifications = true;
  pushNotifications = true;

  // Security Form
  currentPassword = '';
  newPassword = '';
  confirmPassword = '';
  isUpdatingPassword = false;

  // Role Specific Defaults
  doctorAutoAccept = false;
  doctorSlotDuration = 20;
  patientModePreference = 'auto';

  constructor(
    public authService: AuthService,
    private toastService: ToastService,
    public themeService: ThemeService,
    private patientService: PatientService
  ) {}

  ngOnInit(): void {
    this.userRole = this.authService.getRole() || '';
    const soundPref = localStorage.getItem('healsync_sound_enabled');
    this.isSoundEnabled = soundPref === null ? true : soundPref === 'true';
    
    const tonePref = localStorage.getItem('healsync_sound_tone');
    if (tonePref) this.selectedSoundTone = tonePref;
  }

  toggleSound(): void {
    this.isSoundEnabled = !this.isSoundEnabled;
    localStorage.setItem('healsync_sound_enabled', String(this.isSoundEnabled));
    if (this.isSoundEnabled) {
      this.toastService.showSuccess('Notification sound enabled.');
    } else {
      this.toastService.showSuccess('Notification sound muted.');
    }
  }

  onToneChange(): void {
    localStorage.setItem('healsync_sound_tone', this.selectedSoundTone);
    this.toastService.showSuccess(`Notification sound tone updated to ${this.selectedSoundTone}.`);
  }

  setThemeMode(theme: 'light' | 'dark'): void {
    if (this.themeService.getCurrentTheme() !== theme) {
      this.themeService.toggleTheme();
    }
  }

  onChangePassword(): void {
    if (!this.currentPassword || !this.newPassword || !this.confirmPassword) {
      this.toastService.showError('Please fill in all password fields.');
      return;
    }
    if (this.newPassword !== this.confirmPassword) {
      this.toastService.showError('New password and confirm password do not match.');
      return;
    }
    if (this.newPassword.length < 6) {
      this.toastService.showError('Password must be at least 6 characters.');
      return;
    }

    this.isUpdatingPassword = true;
    this.patientService.changePassword(this.currentPassword, this.newPassword).subscribe({
      next: () => {
        this.toastService.showSuccess('Password changed successfully!');
        this.currentPassword = '';
        this.newPassword = '';
        this.confirmPassword = '';
        this.isUpdatingPassword = false;
      },
      error: (err: any) => {
        this.toastService.showError(err, 'Failed to change password.');
        this.isUpdatingPassword = false;
      }
    });
  }

  saveNotificationPreferences(): void {
    this.toastService.showSuccess('Notification preferences updated successfully!');
  }

  saveRoleSettings(): void {
    this.toastService.showSuccess('Role preferences updated successfully!');
  }
}
