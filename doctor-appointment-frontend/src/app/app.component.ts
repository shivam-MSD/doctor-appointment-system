import { Component, OnInit, OnDestroy } from '@angular/core';
import { ThemeService } from './core/services/theme.service';
import { ToastService, ToastMessage } from './core/services/toast.service';
import { AuthService } from './core/services/auth.service';
import { NotificationService } from './core/services/notification.service';
import { Observable, Subscription } from 'rxjs';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent implements OnInit, OnDestroy {
  title = 'doctor-appointment-frontend';
  toasts$: Observable<ToastMessage[]>;
  private authSub?: Subscription;

  constructor(
    private themeService: ThemeService,
    private toastService: ToastService,
    private authService: AuthService,
    private notificationService: NotificationService
  ) {
    this.toasts$ = this.toastService.toasts$;
  }

  ngOnInit(): void {
    // Manage SignalR websocket connection globally based on auth state
    this.authSub = this.authService.currentUser$.subscribe(user => {
      if (user && user.userId) {
        this.notificationService.startConnection(user.userId);
      } else {
        this.notificationService.stopConnection();
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
