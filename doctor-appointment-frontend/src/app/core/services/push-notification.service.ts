import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { SwPush } from '@angular/service-worker';
import { AuthService } from './auth.service';

@Injectable({
  providedIn: 'root'
})
export class PushNotificationService {
  private readonly VAPID_PUBLIC_KEY = 'BEl62iUYgUivxIkv69yViEuiBIa-Ib9-SkvMeA04E01D9J0qU1R3aY4n4e8o-Q_E_G_W_Y';

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
      this.swPush.requestSubscription({
        serverPublicKey: this.VAPID_PUBLIC_KEY
      }).then(sub => {
        const subJson = sub.toJSON();
        const p256dh = subJson.keys ? subJson.keys['p256dh'] : '';
        const auth = subJson.keys ? subJson.keys['auth'] : '';

        const payload = {
          endpoint: sub.endpoint,
          p256dh,
          auth
        };

        this.http.post('/api/notifications/subscribe-push', payload).subscribe({
          next: () => console.log('[Push] Device push token registered successfully.'),
          error: (err) => console.error('[Push] Failed to register subscription:', err)
        });
      }).catch(err => {
        console.warn('[Push] SwPush subscription request skipped:', err);
      });
    }
  }
}
