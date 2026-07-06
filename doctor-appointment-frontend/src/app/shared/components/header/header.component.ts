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
  private pollSub?: Subscription;

  constructor(
    public authService: AuthService,
    public themeService: ThemeService,
    private notificationService: NotificationService,
    private router: Router
  ) {}

  ngOnInit(): void {
    // 1. Initial load
    this.loadNotifications();

    // 2. Poll every 10 seconds for real-time notifications
    this.pollSub = interval(10000).subscribe(() => {
      this.loadNotifications();
    });
  }

  ngOnDestroy(): void {
    if (this.pollSub) {
      this.pollSub.unsubscribe();
    }
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
