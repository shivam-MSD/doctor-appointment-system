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

  showError(error: any, defaultFallbackMsg: string = 'An error occurred', duration: number = 6000): void {
    let parsedMessage = '';
    if (typeof error === 'string') {
      parsedMessage = error;
    } else {
      parsedMessage = this.parseError(error, defaultFallbackMsg);
    }
    this.addToast('error', 'Validation / API Alert', parsedMessage, duration);
  }

  private parseError(err: any, defaultMessage: string): string {
    if (err?.error) {
      const errBody = err.error;

      // 1. Check for ASP.NET Core Validation Errors (errors object)
      if (errBody.errors && typeof errBody.errors === 'object') {
        const messages: string[] = [];
        for (const prop in errBody.errors) {
          if (Object.prototype.hasOwnProperty.call(errBody.errors, prop)) {
            const propErrors = errBody.errors[prop];
            if (Array.isArray(propErrors)) {
              messages.push(...propErrors);
            } else if (typeof propErrors === 'string') {
              messages.push(propErrors);
            }
          }
        }
        if (messages.length > 0) {
          return messages.join(' ');
        }
      }

      // 2. Check for ProblemDetails 'detail'
      if (errBody.detail) {
        return errBody.detail;
      }

      // 3. Check for general message
      if (errBody.message) {
        return errBody.message;
      }

      // 4. Check for direct string body
      if (typeof errBody === 'string') {
        return errBody;
      }
    }

    // 5. Check if the err itself has message/detail (e.g. standard JS Error)
    if (err?.message) {
      return err.message;
    }

    return defaultMessage;
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
