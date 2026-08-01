import { Component, OnInit, OnDestroy, HostListener, ChangeDetectorRef, Output, EventEmitter } from '@angular/core';
import { Router } from '@angular/router';
import { AuthService } from '../../../core/services/auth.service';
import { ThemeService } from '../../../core/services/theme.service';
import { AudioService } from '../../../core/services/audio.service';
import { NotificationService, NotificationDto } from '../../../core/services/notification.service';
import { Subscription, interval } from 'rxjs';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.css']
})
export class HeaderComponent implements OnInit, OnDestroy {
  @Output() toggleMobileMenu = new EventEmitter<void>();
  notifications: NotificationDto[] = [];
  showNotificationsPanel = false;
  showProfilePanel = false;
  currentDateTime: Date = new Date();
  private signalrSub?: Subscription;
  private refreshSub?: Subscription;
  private clockSub?: Subscription;

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement;
    const clickedInsideNotification = target.closest('.notification-container');
    const clickedInsideProfile = target.closest('.profile-container');
    
    if (!clickedInsideNotification) {
      this.showNotificationsPanel = false;
    }
    if (!clickedInsideProfile) {
      this.showProfilePanel = false;
    }
  }

  constructor(
    public authService: AuthService,
    public themeService: ThemeService,
    public audioService: AudioService,
    private notificationService: NotificationService,
    private router: Router,
    private cdr: ChangeDetectorRef
  ) { }

  isAuthPage(): boolean {
    const url = this.router.url.toLowerCase();
    return url.includes('login') || url.includes('register') || url.includes('forgot-password');
  }

  isSoundEnabled(): boolean {
    return this.audioService.isSoundEnabled();
  }

  toggleSound(): void {
    this.audioService.toggleSound();
  }

  ngOnInit(): void {
    // Live clock - update every second
    this.clockSub = interval(1000).subscribe(() => {
      this.currentDateTime = new Date();
    });

    const userId = this.authService.getUserId();
    if (userId) {
      // 1. Initial load
      this.loadNotifications();

      // 2. Listen to incoming push events
      this.notificationService.startConnection(userId);
      this.signalrSub = this.notificationService.notificationReceived$.subscribe(newNotif => {
        this.notifications.unshift(newNotif);
        this.cdr.detectChanges();
      });

      // 3. Auto-sync notifications when app resumes from background on mobile (iOS/Android)
      this.refreshSub = this.notificationService.refreshData$.subscribe(() => {
        this.loadNotifications();
      });
    }
  }

  ngOnDestroy(): void {
    if (this.signalrSub) {
      this.signalrSub.unsubscribe();
    }
    if (this.refreshSub) {
      this.refreshSub.unsubscribe();
    }
    if (this.clockSub) {
      this.clockSub.unsubscribe();
    }
  }

  loadNotifications(): void {
    this.notificationService.getNotifications().subscribe({
      next: (data) => {
        this.notifications = data;
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to load notifications', err)
    });
  }

  getUnreadCount(): number {
    return this.notifications.filter(n => !n.isRead).length;
  }

  toggleNotificationsPanel(): void {
    this.showNotificationsPanel = !this.showNotificationsPanel;
    if (this.showNotificationsPanel) {
      this.showProfilePanel = false;
    }
  }

  toggleProfilePanel(): void {
    this.showProfilePanel = !this.showProfilePanel;
    if (this.showProfilePanel) {
      this.showNotificationsPanel = false;
    }
  }

  markAllAsRead(): void {
    this.notificationService.markAllAsRead().subscribe({
      next: () => {
        this.notifications.forEach(n => n.isRead = true);
        this.cdr.detectChanges();
      }
    });
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }

  getFirstName(): string {
    return this.authService.getFirstName();
  }

  getUserRole(): string {
    return this.authService.getRole() || 'User';
  }

  navigateToProfile(): void {
    this.showProfilePanel = false;
    const role = this.getUserRole().toLowerCase();
    this.router.navigate([`/${role}/profile`]);
  }

  logout(): void {
    this.showProfilePanel = false;
    this.authService.logout();
    const role = this.getUserRole();
    let targetRoute = '/login';
    if (role === 'Admin') targetRoute = '/admin/login';
    if (role === 'SuperAdmin') targetRoute = '/superadmin/login';
    this.router.navigate([targetRoute], { replaceUrl: true });
  }
}
