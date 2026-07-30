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
    const parsedMessage = this.extractErrorMessage(error, defaultFallbackMsg);
    this.addToast('error', 'Alert', parsedMessage, duration);
  }

  public extractErrorMessage(err: any, defaultMessage: string = 'An unexpected error occurred'): string {
    if (!err) return defaultMessage;

    // 1. Intercept HTTP 500-level server errors globally
    if (err?.status >= 500) {
      return 'Our servers are experiencing a temporary issue. Please try again in a few moments.';
    }

    let messageToInspect = '';
    if (typeof err === 'string') {
      messageToInspect = err;
    } else if (err?.error) {
      const errBody = err.error;

      // Check for ASP.NET Core Validation Errors (errors object)
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
          messageToInspect = messages.join(' ');
        }
      }

      // Check for ProblemDetails 'detail'
      if (!messageToInspect && errBody.detail && typeof errBody.detail === 'string') {
        messageToInspect = errBody.detail;
      }

      // Check for general message
      if (!messageToInspect && errBody.message && typeof errBody.message === 'string') {
        messageToInspect = errBody.message;
      }

      // Check for ProblemDetails 'title'
      if (!messageToInspect && errBody.title && typeof errBody.title === 'string' && errBody.title !== 'One or more validation errors occurred.') {
        messageToInspect = errBody.title;
      }

      // Check for direct string body
      if (!messageToInspect && typeof errBody === 'string') {
        messageToInspect = errBody;
      }
    }

    if (!messageToInspect && err?.message && typeof err.message === 'string') {
      messageToInspect = err.message;
    }

    if (!messageToInspect) {
      messageToInspect = defaultMessage;
    }

    // 2. Sanitize any raw C# internal server error text or stack traces
    const lower = messageToInspect.toLowerCase();
    if (
      lower.includes('internal server error') ||
      lower.includes('system.exception') ||
      lower.includes('nullreferenceexception') ||
      lower.includes('argumentexception') ||
      lower.includes('sqlexception') ||
      lower.includes('npgsql') ||
      lower.includes('unhandled exception')
    ) {
      return 'Our servers are experiencing a temporary issue. Please try again in a few moments.';
    }

    return messageToInspect;
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
