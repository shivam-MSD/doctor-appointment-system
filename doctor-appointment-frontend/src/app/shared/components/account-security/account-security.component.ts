import { Component, OnInit } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ToastService } from '../../../core/services/toast.service';

@Component({
  selector: 'app-account-security',
  template: `
    <div style="min-height: 80vh; display: flex; align-items: center; justify-content: center; padding: 20px; text-align: center;">
      <div class="glass-card" style="padding: 40px; max-width: 450px; border-radius: 16px; border: 1px solid var(--border-color); background: var(--bg-card);">
        <span class="spinner" style="display: inline-block; width: 36px; height: 36px; border: 3px solid rgba(6, 182, 212, 0.3); border-top-color: var(--accent-cyan); border-radius: 50%; animation: spin 1s linear infinite; margin-bottom: 16px;"></span>
        <h3 style="margin-bottom: 8px; color: var(--text-primary);">Verifying Account Security...</h3>
        <p style="color: var(--text-muted); font-size: 0.9rem;">Redirecting to your security settings...</p>
      </div>
    </div>
  `
})
export class AccountSecurityComponent implements OnInit {
  constructor(
    private route: ActivatedRoute,
    private router: Router,
    private authService: AuthService,
    private toastService: ToastService
  ) {}

  ngOnInit(): void {
    this.route.queryParams.subscribe(params => {
      const role = params['role'] || 'Patient';
      const email = params['email'] || '';
      const action = params['action'] || 'change-password';

      const activeUser = this.authService.getAnyActiveUser();

      // 1. Unauthenticated Case
      if (!activeUser || !activeUser.token) {
        const returnUrl = `/account/security?role=${role}&action=${action}${email ? '&email=' + encodeURIComponent(email) : ''}`;
        this.toastService.showSuccess('Please log in to review security settings.');
        this.router.navigate(['/login'], {
          queryParams: { role, email, returnUrl },
          replaceUrl: true
        });
        return;
      }

      // 2. Mismatched Logged-in User Case (e.g. Email in alert belongs to another user account)
      if (email && activeUser.email && activeUser.email.toLowerCase() !== email.toLowerCase()) {
        this.toastService.showError(`Logged in as ${activeUser.email}, but security alert belongs to ${email}. Switching accounts...`);
        this.authService.logout();
        const returnUrl = `/account/security?role=${role}&action=${action}&email=${encodeURIComponent(email)}`;
        this.router.navigate(['/login'], {
          queryParams: { role, email, returnUrl },
          replaceUrl: true
        });
        return;
      }

      // 3. Authenticated Matching User Case -> Redirect to Role Profile with Auto-Scroll
      const targetRole = activeUser.role || role;
      const profilePath = `/${targetRole.toLowerCase()}/profile`;
      this.toastService.showSuccess('Opening Security & Password Settings...');
      this.router.navigate([profilePath], {
        queryParams: { action: 'change-password' },
        replaceUrl: true
      });
    });
  }
}
