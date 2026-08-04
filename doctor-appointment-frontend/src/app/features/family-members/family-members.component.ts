import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { FamilyService } from '../../core/services/family.service';
import { ToastService } from '../../core/services/toast.service';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-family-members',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './family-members.component.html',
  styleUrls: ['./family-members.component.css']
})
export class FamilyMembersComponent implements OnInit {
  familyMembers: any[] = [];
  isLoading = true;
  primaryUser: any = null;

  // Add Member Modal State
  showAddModal = false;
  addTab: 'dependent' | 'link' = 'dependent';
  isSubmitting = false;

  // Dependent Form
  dependentForm = {
    firstName: '',
    lastName: '',
    gender: 'Male',
    dob: '',
    relationshipType: 'Child',
    bloodGroup: '',
    consentDeclared: true
  };

  // Link Account OTP Form
  linkForm = {
    targetContact: '',
    channel: 'Both', // "Email", "WhatsApp", "Both"
    relationshipType: 'Spouse',
    otpCode: ''
  };

  otpSent = false;
  otpCooldown = 0;
  private cooldownTimer: any;

  constructor(
    private familyService: FamilyService,
    private toastService: ToastService,
    public authService: AuthService
  ) {}

  ngOnInit(): void {
    this.primaryUser = this.authService.getActiveUserForCurrentRoute();
    this.loadFamilyMembers();
  }

  loadFamilyMembers(): void {
    this.isLoading = true;
    this.familyService.getFamilyMembers().subscribe({
      next: (data) => {
        this.familyMembers = data;
        this.isLoading = false;
      },
      error: () => {
        this.toastService.showError('Failed to load family members.');
        this.isLoading = false;
      }
    });
  }

  openAddModal(): void {
    this.showAddModal = true;
    this.addTab = 'dependent';
    this.otpSent = false;
    this.dependentForm = {
      firstName: '',
      lastName: '',
      gender: 'Male',
      dob: '',
      relationshipType: 'Child',
      bloodGroup: '',
      consentDeclared: true
    };
    this.linkForm = {
      targetContact: '',
      channel: 'Both',
      relationshipType: 'Spouse',
      otpCode: ''
    };
  }

  closeAddModal(): void {
    this.showAddModal = false;
  }

  onAddDependent(): void {
    if (!this.dependentForm.firstName || !this.dependentForm.lastName || !this.dependentForm.dob) {
      this.toastService.showError('Please fill in all required fields (Name & Date of Birth).');
      return;
    }

    if (!this.dependentForm.consentDeclared) {
      this.toastService.showError('You must declare legal guardian consent to add a dependent.');
      return;
    }

    this.isSubmitting = true;
    this.familyService.addDependent(this.dependentForm).subscribe({
      next: (res) => {
        this.toastService.showSuccess(`Added dependent ${res.fullName} successfully!`);
        this.isSubmitting = false;
        this.closeAddModal();
        this.loadFamilyMembers();
      },
      error: (err) => {
        this.toastService.showError(err, 'Failed to add dependent family member.');
        this.isSubmitting = false;
      }
    });
  }

  onSendOtp(): void {
    if (!this.linkForm.targetContact) {
      this.toastService.showError('Please enter target Email ID or WhatsApp mobile number.');
      return;
    }

    this.isSubmitting = true;
    this.familyService.sendFamilyOtp({
      targetContact: this.linkForm.targetContact,
      channel: this.linkForm.channel,
      relationshipType: this.linkForm.relationshipType
    }).subscribe({
      next: (res) => {
        this.toastService.showSuccess(res.message || `OTP dispatched via ${this.linkForm.channel}!`);
        if (res.demoOtpCode) {
          this.toastService.showSuccess(`DEMO OTP CODE: ${res.demoOtpCode}`);
        }
        this.otpSent = true;
        this.isSubmitting = false;
        this.startCooldown();
      },
      error: (err) => {
        this.toastService.showError(err, 'Failed to send OTP.');
        this.isSubmitting = false;
      }
    });
  }

  onVerifyOtp(): void {
    if (!this.linkForm.otpCode || this.linkForm.otpCode.length < 4) {
      this.toastService.showError('Please enter a valid 6-digit OTP code.');
      return;
    }

    this.isSubmitting = true;
    this.familyService.verifyFamilyOtp({
      targetContact: this.linkForm.targetContact,
      otpCode: this.linkForm.otpCode,
      relationshipType: this.linkForm.relationshipType
    }).subscribe({
      next: (res) => {
        this.toastService.showSuccess(`Linked ${res.fullName}'s account to your family hub!`);
        this.isSubmitting = false;
        this.closeAddModal();
        this.loadFamilyMembers();
      },
      error: (err) => {
        this.toastService.showError(err, 'Invalid OTP code.');
        this.isSubmitting = false;
      }
    });
  }

  startCooldown(): void {
    this.otpCooldown = 60;
    if (this.cooldownTimer) clearInterval(this.cooldownTimer);
    this.cooldownTimer = setInterval(() => {
      if (this.otpCooldown > 0) {
        this.otpCooldown--;
      } else {
        clearInterval(this.cooldownTimer);
      }
    }, 1000);
  }

  onDeleteMember(member: any): void {
    if (confirm(`Are you sure you want to remove ${member.fullName} from your family hub?`)) {
      this.familyService.deleteFamilyMember(member.patientId).subscribe({
        next: () => {
          this.toastService.showSuccess('Family member removed.');
          this.loadFamilyMembers();
        },
        error: (err) => this.toastService.showError(err, 'Failed to remove family member.')
      });
    }
  }

  getInitials(name: string): string {
    if (!name) return 'FM';
    const parts = name.trim().split(' ');
    if (parts.length >= 2) return `${parts[0][0]}${parts[1][0]}`.toUpperCase();
    return name.substring(0, 2).toUpperCase();
  }
}
