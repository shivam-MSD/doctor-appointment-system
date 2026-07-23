import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, Subject } from 'rxjs';
import { HubConnection, HubConnectionBuilder, HubConnectionState } from '@microsoft/signalr';

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

  constructor(private http: HttpClient) { }

  getNotifications(): Observable<NotificationDto[]> {
    return this.http.get<NotificationDto[]>('/api/notifications');
  }

  markAllAsRead(): Observable<any> {
    return this.http.post<any>('/api/notifications/mark-read', {});
  }

  // startConnection(userId: string): void {
  //   if (this.hubConnection && (this.hubConnection.state === HubConnectionState.Connected || this.hubConnection.state === HubConnectionState.Connecting)) {
  //     return;
  //   }

  //   let hubUrl = `/notificationHub?userId=${userId}`;
  //   if (window.location.hostname === 'localhost' || window.location.hostname === '127.0.0.1') {
  //     hubUrl = `http://localhost:5222/notificationHub?userId=${userId}`;
  //   }

  //   this.hubConnection = new HubConnectionBuilder()
  //     .withUrl(hubUrl)
  //     .withAutomaticReconnect()
  //     .build();

  //   this.hubConnection.on('ReceiveNotification', (notification: NotificationDto) => {
  //     this.notificationReceivedSource.next(notification);
  //   });

  //   this.hubConnection.on('RefreshData', (dataArea: string) => {
  //     this.refreshSource.next(dataArea);
  //   });

  //   this.hubConnection.start()
  //     .then(() => console.log('SignalR NotificationHub connection started.'))
  //     .catch(err => console.error('Error starting SignalR connection:', err));
  // }

  // stopConnection(): void {
  //   if (this.hubConnection) {
  //     this.hubConnection.stop()
  //       .then(() => console.log('SignalR connection stopped.'))
  //       .catch(err => console.error('Error stopping SignalR connection:', err));
  //   }
  // }

  startConnection(userId: string): void {
    if (this.hubConnection && (this.hubConnection.state === HubConnectionState.Connected || this.hubConnection.state === HubConnectionState.Connecting)) {
      return;
    }

    this.hubConnection = new HubConnectionBuilder()
      .withUrl(`/notificationHub?userId=${userId}`)
      .withAutomaticReconnect()
      .build();

    this.hubConnection.on('ReceiveNotification', (notification: NotificationDto) => {
      this.notificationReceivedSource.next(notification);
    });

    this.hubConnection.on('RefreshData', (dataArea: string) => {
      this.refreshSource.next(dataArea);
    });

    this.hubConnection.start()
      .then(() => console.log('SignalR NotificationHub connection started.'))
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
