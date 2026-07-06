import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';

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
  constructor(private http: HttpClient) {}

  getNotifications(): Observable<NotificationDto[]> {
    return this.http.get<NotificationDto[]>('/api/notifications');
  }

  markAllAsRead(): Observable<any> {
    return this.http.post<any>('/api/notifications/mark-read', {});
  }
}
