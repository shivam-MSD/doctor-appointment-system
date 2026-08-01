import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';
import { environment } from 'src/environments/environment';
import { AudioService } from './audio.service';

export interface NotificationDto {
  notificationId: string;
  message: string;
  isRead: boolean;
  createdDate: string;
}

@Injectable({
  providedIn: 'root'
})
export class NotificationService {
  private hubConnection?: HubConnection;
  private notificationReceivedSource = new Subject<NotificationDto>();
  public notificationReceived$ = this.notificationReceivedSource.asObservable();
  private refreshSource = new Subject<string>();
  public refreshData$ = this.refreshSource.asObservable();
  private currentUserId: string = '';

  constructor(
    private http: HttpClient,
    private audioService: AudioService
  ) {
    this.setupAppLifecycleListeners();
  }

  getNotifications(): Observable<NotificationDto[]> {
    return this.http.get<NotificationDto[]>('/api/notifications');
  }

  markAllAsRead(): Observable<any> {
    return this.http.post<any>('/api/notifications/mark-read', {});
  }

  requestNotificationPermission(): Promise<NotificationPermission> {
    if (typeof window !== 'undefined' && 'Notification' in window) {
      return Notification.requestPermission();
    }
    return Promise.resolve('denied' as NotificationPermission);
  }

  startConnection(userId: string): void {
    if (!userId) return;
    this.currentUserId = userId;

    if (this.hubConnection && (this.hubConnection.state === HubConnectionState.Connected || this.hubConnection.state === HubConnectionState.Connecting)) {
      return;
    }

    // Defer notification permission check on iOS/Android cold boot to prevent freezing initial render thread
    if (typeof window !== 'undefined' && 'Notification' in window && Notification.permission === 'default') {
      setTimeout(() => {
        this.requestNotificationPermission().catch(() => { });
      }, 1500);
    }

    const baseUrl = environment.apiUrl ? environment.apiUrl : '';
    const token = localStorage.getItem('healsync_auth_Doctor') ||
                  localStorage.getItem('healsync_auth_Patient') ||
                  localStorage.getItem('healsync_auth_Admin') ||
                  localStorage.getItem('healsync_auth_SuperAdmin');
    let tokenStr = '';
    if (token) {
      try { tokenStr = JSON.parse(token).token || ''; } catch { }
    }

    const hubUrl = `${baseUrl}/notificationHub?userId=${userId}`;

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => tokenStr,
        transport: 1 | 4 // WebSockets (1) | LongPolling (4)
      })
      .withAutomaticReconnect([0, 2000, 5000, 10000, 30000])
      .build();

    // Auto-reconnect lifecycle handlers
    this.hubConnection.onreconnected(() => {
      console.log('[SignalR] Connection re-established. Triggering auto-sync...');
      this.refreshSource.next('All');
    });

    this.hubConnection.onclose(() => {
      console.warn('[SignalR] Connection closed. Attempting reconnect if app is active...');
      if (document.visibilityState === 'visible' && this.currentUserId) {
        setTimeout(() => this.startConnection(this.currentUserId), 3000);
      }
    });

    this.hubConnection.on('ReceiveNotification', (notification: NotificationDto) => {
      // 1. Play audio chime sound and trigger mobile device vibration
      try {
        this.audioService.playNotificationSound();
      } catch { }

      // 2. Trigger native OS System Notification popup (Android, iOS Safari PWA, Windows PC)
      this.showNativeSystemNotification(notification);

      this.notificationReceivedSource.next(notification);
    });

    this.hubConnection.on('RefreshData', (dataArea: string) => {
      this.refreshSource.next(dataArea);
    });

    this.hubConnection.start()
      .then(() => {
        console.log('SignalR NotificationHub connection established successfully.');
        // Trigger initial sync on connect
        this.refreshSource.next('All');
      })
      .catch(err => console.error('Error starting SignalR connection:', err));
  }

  stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.stop()
        .then(() => console.log('SignalR connection stopped.'))
        .catch(err => console.error('Error stopping SignalR connection:', err));
    }
  }

  /**
   * Triggers native OS System Notification banner on Android Chrome, iOS Safari PWA, and Desktop
   */
  private showNativeSystemNotification(notification: NotificationDto): void {
    try {
      if (typeof window === 'undefined' || !('Notification' in window) || Notification.permission !== 'granted') {
        return;
      }

      const cleanText = notification.message ? notification.message.replace(/<[^>]*>?/gm, '') : 'New notification received';
      const options: NotificationOptions = {
        body: cleanText,
        icon: '/assets/logo-192.png',
        badge: '/assets/logo-192.png',
        tag: notification.notificationId || 'healsync-notification',
        renotify: true
      };

      // Service Worker registration (Primary method for Android & iOS 16.4+ PWAs)
      if ('serviceWorker' in navigator) {
        navigator.serviceWorker.ready.then(reg => {
          reg.showNotification('HealSync Medical Network', options).catch(() => {
            // Fallback to standard window Notification if SW showNotification fails
            new Notification('HealSync Medical Network', options);
          });
        }).catch(() => {
          new Notification('HealSync Medical Network', options);
        });
      } else {
        new Notification('HealSync Medical Network', options);
      }
    } catch (e) {
      console.warn('Failed to display native system notification banner:', e);
    }
  }

  /**
   * Listens for mobile app resume (visibilitychange, focus, online)
   * Automatically reconnects SignalR and triggers silent data refetch without requiring manual refresh!
   */
  private setupAppLifecycleListeners(): void {
    if (typeof window === 'undefined') return;

    const onAppResume = () => {
      if (document.visibilityState === 'visible') {
        console.log('[Mobile PWA Lifecycle] App resumed / focused. Checking SignalR state...');

        // 1. Re-establish SignalR connection if disconnected
        if (this.currentUserId) {
          if (!this.hubConnection || this.hubConnection.state === HubConnectionState.Disconnected) {
            this.startConnection(this.currentUserId);
          }
        }

        // 2. Trigger automatic silent data refresh for UI components (Header, Dashboard, Notifications)
        this.refreshSource.next('All');
      }
    };

    document.addEventListener('visibilitychange', onAppResume);
    window.addEventListener('focus', onAppResume);
    window.addEventListener('online', onAppResume);
  }
}
