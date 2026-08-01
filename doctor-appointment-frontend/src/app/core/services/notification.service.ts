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

  constructor(
    private http: HttpClient,
    private audioService: AudioService
  ) { }

  getNotifications(): Observable<NotificationDto[]> {
    return this.http.get<NotificationDto[]>('/api/notifications');
  }

  markAllAsRead(): Observable<any> {
    return this.http.post<any>('/api/notifications/mark-read', {});
  }

  startConnection(userId: string): void {
    if (!userId) return;

    if (this.hubConnection && (this.hubConnection.state === HubConnectionState.Connected || this.hubConnection.state === HubConnectionState.Connecting)) {
      return;
    }

    // Request native browser OS notification permission if default
    if (typeof window !== 'undefined' && 'Notification' in window && Notification.permission === 'default') {
      Notification.requestPermission().catch(() => { });
    }

    const baseUrl = environment.apiUrl ? environment.apiUrl : '';
    const token = localStorage.getItem('healsync_auth_Doctor') || localStorage.getItem('healsync_auth_Patient') || localStorage.getItem('healsync_auth_Admin') || localStorage.getItem('healsync_auth_SuperAdmin');
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

    this.hubConnection.on('ReceiveNotification', (notification: NotificationDto) => {
      // 1. Play audio chime sound and trigger mobile device vibration
      try {
        this.audioService.playNotificationSound();
      } catch { }

      // 2. Trigger native OS System Notification popup (Windows PC / Android / iOS)
      try {
        if (typeof window !== 'undefined' && 'Notification' in window && Notification.permission === 'granted') {
          const cleanText = notification.message ? notification.message.replace(/<[^>]*>?/gm, '') : 'New notification received';
          new Notification('HealSync Medical Network', {
            body: cleanText,
            icon: '/assets/logo-192.png',
            badge: '/assets/logo-192.png'
          });
        }
      } catch { }

      this.notificationReceivedSource.next(notification);
    });

    this.hubConnection.on('RefreshData', (dataArea: string) => {
      this.refreshSource.next(dataArea);
    });

    this.hubConnection.start()
      .then(() => console.log('SignalR NotificationHub connection established successfully.'))
      .catch(err => console.error('Error starting SignalR connection:', err));
  }

  stopConnection(): void {
    if (this.hubConnection) {
      this.hubConnection.stop()
        .then(() => console.log('SignalR connection stopped.'))
        .catch(err => console.error('Error stopping SignalR connection:', err));
    }
  }
}
