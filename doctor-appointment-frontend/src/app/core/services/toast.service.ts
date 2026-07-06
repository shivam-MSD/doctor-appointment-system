import { Injectable } from '@angular/core';
import { BehaviorSubject, Observable } from 'rxjs';

export interface ToastMessage {
  id: number;
  type: 'success' | 'error';
  title: string;
  message: string;
}

@Injectable({
  providedIn: 'root'
})
export class ToastService {
  private toastsSubject = new BehaviorSubject<ToastMessage[]>([]);
  toasts$: Observable<ToastMessage[]> = this.toastsSubject.asObservable();
  private counter = 0;

  showSuccess(message: string, title: string = 'Success', duration: number = 4000): void {
    this.addToast('success', title, message, duration);
  }

  showError(message: string, title: string = 'Validation Alert', duration: number = 6000): void {
    this.addToast('error', title, message, duration);
  }

  private addToast(type: 'success' | 'error', title: string, message: string, duration: number): void {
    const id = ++this.counter;
    const newToast: ToastMessage = { id, type, title, message };
    const currentToasts = this.toastsSubject.value;
    this.toastsSubject.next([...currentToasts, newToast]);

    if (duration > 0) {
      setTimeout(() => {
        this.remove(id);
      }, duration);
    }
  }

  remove(id: number): void {
    const filtered = this.toastsSubject.value.filter(t => t.id !== id);
    this.toastsSubject.next(filtered);
  }
}
