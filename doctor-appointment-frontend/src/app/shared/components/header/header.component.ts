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
  selectedCity: string = localStorage.getItem('user_preferred_city') || 'Vadodara';
  currentDateTime: Date = new Date();

  onCitySelected(city: string): void {
    this.selectedCity = city;
    localStorage.setItem('user_preferred_city', city);
    window.dispatchEvent(new CustomEvent('cityChanged', { detail: city }));
  }
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
        this.notifications = this.deduplicateNotifications(data);
        this.cdr.detectChanges();
      },
      error: (err) => console.error('Failed to load notifications', err)
    });
  }

  deduplicateNotifications(items: NotificationDto[]): NotificationDto[] {
    const seen = new Set<string>();
    return items.filter(item => {
      const key = item.notificationId ? item.notificationId : `${item.message}_${item.createdDate}`;
      if (seen.has(key)) return false;
      seen.add(key);
      return true;
    });
  }

  getNotificationIcon(message: string): string {
    if (!message) return 'bell';
    const msg = message.toLowerCase();
    if (msg.includes('approved') || msg.includes('confirmed') || msg.includes('completed')) return 'check-circle-2';
    if (msg.includes('cancelled') || msg.includes('rejected') || msg.includes('declined')) return 'x-circle';
    if (msg.includes('reschedule') || msg.includes('proposed') || msg.includes('time')) return 'calendar-clock';
    return 'bell';
  }

  getNotificationIconColor(message: string): string {
    if (!message) return '#243B63';
    const msg = message.toLowerCase();
    if (msg.includes('approved') || msg.includes('confirmed') || msg.includes('completed')) return '#059669';
    if (msg.includes('cancelled') || msg.includes('rejected') || msg.includes('declined')) return '#dc2626';
    if (msg.includes('reschedule') || msg.includes('proposed') || msg.includes('time')) return '#b45309';
    return '#243B63';
  }

  getTimeAgo(dateInput: string | Date): string {
    if (!dateInput) return '';
    const date = new Date(dateInput);
    const now = new Date();
    const diffMs = now.getTime() - date.getTime();
    const diffMins = Math.floor(diffMs / (1000 * 60));
    const diffHours = Math.floor(diffMs / (1000 * 60 * 60));
    const diffDays = Math.floor(diffMs / (1000 * 60 * 60 * 24));

    if (diffMins < 1) return 'Just now';
    if (diffMins < 60) return `${diffMins}m ago`;
    if (diffHours < 24) return `${diffHours}h ago`;
    if (diffDays === 1) return 'Yesterday';
    if (diffDays < 7) return `${diffDays}d ago`;

    return date.toLocaleDateString('en-US', { month: 'short', day: 'numeric' });
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
