import { Component, OnInit, OnDestroy } from '@angular/core';
import { ThemeService } from './core/services/theme.service';
import { ToastService, ToastMessage } from './core/services/toast.service';
import { AuthService } from './core/services/auth.service';
import { NotificationService } from './core/services/notification.service';
import { LoadingService } from './core/services/loading.service';
import { PwaService } from './core/services/pwa.service';
import { PushNotificationService } from './core/services/push-notification.service';
import { Observable, Subscription } from 'rxjs';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'doctor-appointment-frontend';
  toasts$: Observable<ToastMessage[]>;
  isLoading$: Observable<boolean>;
  showPwaPrompt$: Observable<boolean>;
  private authSub?: Subscription;

  constructor(
    private themeService: ThemeService,
    private toastService: ToastService,
    private authService: AuthService,
    private notificationService: NotificationService,
    private loadingService: LoadingService,
    public pwaService: PwaService,
    private pushNotificationService: PushNotificationService
  ) {
    this.toasts$ = this.toastService.toasts$;
    this.isLoading$ = this.loadingService.isLoading$;
    this.showPwaPrompt$ = this.pwaService.showPrompt$;
  }

  ngOnInit(): void {
    // Manage SignalR websocket connection globally based on auth state
    this.authSub = this.authService.currentUser$.subscribe(user => {
      if (user && user.userId) {
        this.notificationService.startConnection(user.userId);
        this.pushNotificationService.requestSubscription();
      } else {
        this.notificationService.stopConnection();
      }
    });

    // Real-time cross-tab Login & Logout Auto-Sync
    window.addEventListener('storage', (event: StorageEvent) => {
      if (event.key && event.key.startsWith('healsync_auth_')) {
        const roleFromKey = event.key.replace('healsync_auth_', ''); // 'Doctor', 'Patient', 'Admin', 'SuperAdmin'
        const currentPath = window.location.pathname.toLowerCase();

        // 1. LOGOUT Event: If user logged out on another tab for the role matching current route
        if (!event.newValue) {
          if (currentPath.includes(`/${roleFromKey.toLowerCase()}`)) {
            let loginPath = '/login';
            if (roleFromKey === 'Admin') loginPath = '/admin/login';
            if (roleFromKey === 'SuperAdmin') loginPath = '/superadmin/login';
            this.authService.logout(roleFromKey);
            window.location.href = loginPath;
          }
        } 
        // 2. LOGIN Event: If user logged in on another tab for a role
        else if (event.newValue) {
          if (currentPath === '/login' || currentPath.includes('/login')) {
            try {
              const parsedUser = JSON.parse(event.newValue);
              if (parsedUser && parsedUser.role) {
                const dashPath = `/${parsedUser.role.toLowerCase()}/dashboard`;
                window.location.href = dashPath;
              }
            } catch { }
          }
        }
      }
    });

    // Global Swipe-to-Dismiss for Mobile Bottom Sheet Modals
    this.setupSwipeToDismiss();
  }

  private setupSwipeToDismiss(): void {
    let startY = 0;
    let currentY = 0;
    let isDragging = false;
    let modalEl: HTMLElement | null = null;

    document.addEventListener('touchstart', (e: TouchEvent) => {
      const target = e.target as HTMLElement;
      // Only trigger on the top 60px of a modal-container (the drag handle area)
      const container = target.closest('.modal-container') as HTMLElement;
      if (!container) return;

      const rect = container.getBoundingClientRect();
      const touchY = e.touches[0].clientY;

      // Only allow swipe if touching the top 60px area (drag handle zone)
      if (touchY - rect.top > 60) return;

      startY = touchY;
      currentY = touchY;
      isDragging = true;
      modalEl = container;
      modalEl.style.transition = 'none';
    }, { passive: true });

    document.addEventListener('touchmove', (e: TouchEvent) => {
      if (!isDragging || !modalEl) return;
      currentY = e.touches[0].clientY;
      const deltaY = currentY - startY;

      // Only allow downward drag
      if (deltaY > 0) {
        modalEl.style.transform = `translateY(${deltaY}px)`;
        modalEl.style.opacity = `${Math.max(0.4, 1 - deltaY / 400)}`;
      }
    }, { passive: true });

    document.addEventListener('touchend', () => {
      if (!isDragging || !modalEl) return;
      const deltaY = currentY - startY;
      const dismissTarget = modalEl; // Capture reference before nulling

      modalEl.style.transition = 'transform 0.3s ease, opacity 0.3s ease';

      if (deltaY > 80) {
        // Swipe threshold reached — dismiss
        dismissTarget.style.transform = 'translateY(100%)';
        dismissTarget.style.opacity = '0';
        setTimeout(() => {
          // Click the backdrop to trigger Angular's close handler
          const backdrop = dismissTarget.closest('.modal-backdrop') as HTMLElement;
          if (backdrop) backdrop.click();
          // Reset styles
          dismissTarget.style.transform = '';
          dismissTarget.style.opacity = '';
          dismissTarget.style.transition = '';
        }, 250);
      } else {
        // Not enough swipe — snap back
        dismissTarget.style.transform = 'translateY(0)';
        dismissTarget.style.opacity = '1';
        setTimeout(() => {
          dismissTarget.style.transform = '';
          dismissTarget.style.opacity = '';
          dismissTarget.style.transition = '';
        }, 300);
      }

      isDragging = false;
      modalEl = null;
    }, { passive: true });
  }

  ngOnDestroy(): void {
    if (this.authSub) {
      this.authSub.unsubscribe();
    }
  }

  dismissToast(id: number): void {
    this.toastService.removeToast(id);
  }

  removeToast(id: number): void {
    this.toastService.removeToast(id);
  }
}
