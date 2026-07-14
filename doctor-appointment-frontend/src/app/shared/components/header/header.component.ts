import { Component, OnInit, OnDestroy, HostListener, ChangeDetectorRef } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';
import { NotificationService, NotificationDto } from '../../../core/services/notification.service';
import { Subscription, interval, timer } from 'rxjs';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.css']
})
export class HeaderComponent implements OnInit, OnDestroy {
  notifications: NotificationDto[] = [];
  showNotificationsPanel = false;
  currentDateTime: Date = new Date();
  private signalrSub?: Subscription;
  private clockSub?: Subscription;

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    const clickedInside = target.closest('.notification-container');
    if (!clickedInside) {
      this.showNotificationsPanel = false;
    }
  }

  constructor(
    public authService: AuthService,
    public themeService: ThemeService,
    private notificationService: NotificationService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) { }

  ngOnInit(): void {
    // Live clock - update every second
    this.clockSub = interval(1000).subscribe(() => {
      this.currentDateTime = new Date();
    });

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
    if (this.clockSub) {
      this.clockSub.unsubscribe();
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
