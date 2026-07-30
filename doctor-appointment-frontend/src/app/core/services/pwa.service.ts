import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

@Injectable({
  providedIn: 'root'
})
export class PwaService {
  private deferredPrompt: any = null;
  private showPromptSubject = new BehaviorSubject<boolean>(false);
  public showPrompt$: Observable<boolean> = this.showPromptSubject.asObservable();

  constructor() {
    this.initPwaListeners();
  }

  private initPwaListeners(): void {
    // 1. STRICT MOBILE ONLY: Never show PWA banners or trigger install popups on Desktop!
    if (!this.isMobileDevice()) {
      this.showPromptSubject.next(false);
      return;
    }

    // Check if running in standalone display mode (already opened inside installed mobile PWA app)
    const isStandalone = window.matchMedia('(display-mode: standalone)').matches ||
                          (window.navigator as any).standalone === true;

    if (isStandalone) {
      this.showPromptSubject.next(false);
      return;
    }

    // Check if user already dismissed prompt in this session
    const dismissed = sessionStorage.getItem('pwa_prompt_dismissed');
    if (dismissed) {
      this.showPromptSubject.next(false);
      return;
    }

    // Listen for mobile browser install event
    window.addEventListener('beforeinstallprompt', (e: Event) => {
      e.preventDefault();
      this.deferredPrompt = e;
      this.showPromptSubject.next(true);
    });
  }

  isMobileDevice(): boolean {
    return /Android|webOS|iPhone|iPad|iPod|BlackBerry|IEMobile|Opera Mini/i.test(navigator.userAgent);
  }

  openPwaApp(): void {
    if (this.deferredPrompt) {
      this.deferredPrompt.prompt();
      this.deferredPrompt.userChoice.then((choiceResult: any) => {
        if (choiceResult.outcome === 'accepted') {
          console.log('User accepted the PWA install prompt');
        }
        this.deferredPrompt = null;
        this.dismissPrompt();
      });
    } else {
      // Fallback: reload in root path or guide user
      window.location.href = '/login';
    }
  }

  dismissPrompt(): void {
    sessionStorage.setItem('pwa_prompt_dismissed', 'true');
    this.showPromptSubject.next(false);
  }
}
