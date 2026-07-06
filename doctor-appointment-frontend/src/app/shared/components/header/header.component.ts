import { Component, OnInit, OnDestroy } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';
import { NotificationService, NotificationDto } from '../../../core/services/notification.service';
import { Subscription, interval } from 'rxjs';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.css']
})
export class HeaderComponent implements OnInit, OnDestroy {
  notifications: NotificationDto[] = [];
  showNotificationsPanel = false;
  private signalrSub?: Subscription;

  constructor(
    public authService: AuthService,
    public themeService: ThemeService,
    private notificationService: NotificationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    const userId = this.authService.getUserId();
    if (userId) {
      // 1. Initial load
      this.loadNotifications();

      // 2. Start SignalR real-time websocket channel
      this.notificationService.startConnection(userId);

      // 3. Listen to incoming push events
      this.signalrSub = this.notificationService.notificationReceived$.subscribe({
        next: (notification: NotificationDto) => {
          this.notifications = [notification, ...this.notifications];
        }
      });
    }
  }

  ngOnDestroy(): void {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
    this.notificationService.stopConnection();
  }

  loadNotifications(): void {
    if (!this.authService.getUserId()) return;

    this.notificationService.getNotifications().subscribe({
      next: (res) => {
        this.notifications = res;
      },
      error: () => {
        // Fail silently to prevent console pollution
      }
    });
  }

  toggleNotificationsPanel(): void {
    this.showNotificationsPanel = !this.showNotificationsPanel;
    if (this.showNotificationsPanel) {
      this.loadNotifications();
    }
  }

  markAllAsRead(): void {
    this.notificationService.markAllAsRead().subscribe({
      next: () => {
        this.notifications = this.notifications.map(n => ({ ...n, isRead: true }));
        this.showNotificationsPanel = false;
      }
    });
  }

  getUnreadCount(): number {
    return this.notifications.filter(n => !n.isRead).length;
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  logout(): void {
    const role = this.authService.getRole();
    this.notificationService.stopConnection();
    this.authService.logout();

    if (role === 'Doctor') {
      this.router.navigate(['/doctor/login']);
    } else if (role === 'Admin') {
      this.router.navigate(['/admin/login']);
    } else if (role === 'SuperAdmin') {
      this.router.navigate(['/superadmin/login']);
    } else {
      this.router.navigate(['/patient/login']);
    }
  }
}
