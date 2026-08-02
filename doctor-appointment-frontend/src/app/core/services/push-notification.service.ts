import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { SwPush } from '@angular/service-worker';
import { AuthService } from './auth.service';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class PushNotificationService {
  private readonly apiUrl = environment.apiUrl;

  constructor(
    private http: HttpClient,
    private swPush: SwPush,
    private authService: AuthService
  ) {}

  public requestSubscription(): void {
    if (typeof Notification === 'undefined') {
      return;
    }

    if (Notification.permission === 'granted') {
      this.subscribeToPush();
    } else if (Notification.permission !== 'denied') {
      Notification.requestPermission().then((permission) => {
        if (permission === 'granted') {
          this.subscribeToPush();
        }
      });
    }
  }

  private subscribeToPush(): void {
    const userId = this.authService.getUserId();
    if (!userId) return;

    if (this.swPush.isEnabled) {
      this.http.get<{ publicKey: string }>(`${this.apiUrl}/notifications/vapid-public-key`).subscribe({
        next: (res) => {
          if (!res?.publicKey) return;

          this.swPush.requestSubscription({
            serverPublicKey: res.publicKey
          }).then(sub => {
            const subJson = sub.toJSON();
            const p256dh = subJson.keys ? subJson.keys['p256dh'] : '';
            const auth = subJson.keys ? subJson.keys['auth'] : '';

            const payload = {
              endpoint: sub.endpoint,
              p256dh,
              auth
            };

            this.http.post(`${this.apiUrl}/notifications/subscribe-push`, payload).subscribe({
              next: () => console.log('[Push] Device push token registered successfully.'),
              error: (err) => console.error('[Push] Failed to register subscription:', err)
            });
          }).catch(err => {
            console.warn('[Push] SwPush subscription request skipped:', err);
          });
        },
        error: (err) => console.warn('[Push] Failed to fetch VAPID public key:', err)
      });
    }
  }
}
