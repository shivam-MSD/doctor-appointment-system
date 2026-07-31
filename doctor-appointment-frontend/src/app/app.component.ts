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
            const loginPath = `/${roleFromKey.toLowerCase()}/login`;
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
  }

  ngOnDestroy(): void {
    if (this.authSub) {
      this.authSub.unsubscribe();
    }
    this.notificationService.stopConnection();
  }

  removeToast(id: number): void {
    this.toastService.remove(id);
  }
}
