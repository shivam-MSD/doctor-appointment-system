import { Component } from '@angular/core';
import { ThemeService } from './core/services/theme.service';
import { ToastService, ToastMessage } from './core/services/toast.service';
import { Observable } from 'rxjs';

@Component({
  selector: 'app-root',
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'doctor-appointment-frontend';
  toasts$: Observable<ToastMessage[]>;

  constructor(
    private themeService: ThemeService,
    private toastService: ToastService
  ) {
    this.toasts$ = this.toastService.toasts$;
  }

  removeToast(id: number): void {
    this.toastService.remove(id);
  }
}
