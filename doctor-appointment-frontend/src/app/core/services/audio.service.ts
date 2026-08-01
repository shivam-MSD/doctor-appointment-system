import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class AudioService {
  private notificationAudio: HTMLAudioElement | null = null;
  private audioUnlocked = false;
  private audioContext: AudioContext | null = null;
  private soundEnabledKey = 'healsync_sound_enabled';

  constructor() {
    this.initAudio();
    this.setupGestureListeners();
  }

  private initAudio(): void {
    try {
      this.notificationAudio = new Audio('/assets/audio/notification.mp3');
      this.notificationAudio.preload = 'auto';
    } catch (e) {
      console.warn('HTML5 Audio initialization fallback', e);
    }
  }

  /**
   * User preference controls: Check if notification sound is enabled.
   */
  public isSoundEnabled(): boolean {
    const pref = localStorage.getItem(this.soundEnabledKey);
    return pref === null ? true : pref === 'true';
  }

  /**
   * Enable or disable sound notifications according to user preference.
   */
  public setSoundEnabled(enabled: boolean): void {
    localStorage.setItem(this.soundEnabledKey, enabled ? 'true' : 'false');
  }

  /**
   * Toggle sound mute/unmute state.
   */
  public toggleSound(): boolean {
    const newState = !this.isSoundEnabled();
    this.setSoundEnabled(newState);
    if (newState) {
      this.playNotificationSound(true); // Play brief confirmation chime when unmuted
    }
    return newState;
  }

  /**
   * Listens for initial user interaction (tap, touch, click, keydown)
   * to unlock browser Web Audio & Audio autoplay policies on mobile iOS/Android devices.
   */
  private setupGestureListeners(): void {
    const unlock = () => {
      if (this.audioUnlocked) return;

      // 1. Prime HTML5 Audio
      if (this.notificationAudio) {
        this.notificationAudio.play().then(() => {
          if (this.notificationAudio) {
            this.notificationAudio.pause();
            this.notificationAudio.currentTime = 0;
          }
          this.audioUnlocked = true;
        }).catch(() => { });
      }

      // 2. Prime Web Audio API Context
      try {
        const AudioCtx = window.AudioContext || (window as any).webkitAudioContext;
        if (AudioCtx) {
          this.audioContext = new AudioCtx();
          if (this.audioContext.state === 'suspended') {
            this.audioContext.resume();
          }
        }
      } catch { }

      window.removeEventListener('touchstart', unlock);
      window.removeEventListener('click', unlock);
      window.removeEventListener('keydown', unlock);
    };

    window.addEventListener('touchstart', unlock, { once: true });
    window.addEventListener('click', unlock, { once: true });
    window.addEventListener('keydown', unlock, { once: true });
  }

  /**
   * Plays the notification chime sound and triggers mobile device vibration.
   * Respects the user's sound preference unless force = true.
   */
  public playNotificationSound(force: boolean = false): void {
    if (!force && !this.isSoundEnabled()) {
      return; // Silent when user has muted sound notifications
    }

    // 1. Play Mobile Tactile Vibration
    try {
      if (typeof navigator !== 'undefined' && 'vibrate' in navigator) {
        navigator.vibrate([200, 100, 200]);
      }
    } catch { }

    // 2. Play HTML5 Audio Chime
    if (this.notificationAudio) {
      this.notificationAudio.currentTime = 0;
      this.notificationAudio.play().catch(() => {
        // Fallback to Web Audio API synthesis if HTML5 Audio fails
        this.playWebAudioChime();
      });
      return;
    }

    // 3. Fallback to Web Audio Synth
    this.playWebAudioChime();
  }

  /**
   * Synthesizes a clean dual-frequency chime sound using Web Audio API
   * as a fail-safe fallback for mobile browsers.
   */
  private playWebAudioChime(): void {
    try {
      const AudioCtx = window.AudioContext || (window as any).webkitAudioContext;
      if (!AudioCtx) return;
      const ctx = this.audioContext || new AudioCtx();
      
      if (ctx.state === 'suspended') {
        ctx.resume();
      }

      const now = ctx.currentTime;
      
      // Dual oscillator tone (880 Hz + 1318.5 Hz)
      const osc1 = ctx.createOscillator();
      const osc2 = ctx.createOscillator();
      const gainNode = ctx.createGain();

      osc1.type = 'sine';
      osc1.frequency.setValueAtTime(880.0, now); // A5

      osc2.type = 'sine';
      osc2.frequency.setValueAtTime(1318.51, now); // E6

      gainNode.gain.setValueAtTime(0.3, now);
      gainNode.gain.exponentialRampToValueAtTime(0.001, now + 0.5);

      osc1.connect(gainNode);
      osc2.connect(gainNode);
      gainNode.connect(ctx.destination);

      osc1.start(now);
      osc2.start(now);
      osc1.stop(now + 0.5);
      osc2.stop(now + 0.5);
    } catch { }
  }
}
