import { Component, OnInit } from '@angular/core';
import { Router } from '@angular/router';

@Component({
  selector: 'app-reset-password',
  template: '<p>Redirecting to forgot-password...</p>'
})
export class ResetPasswordComponent implements OnInit {
  constructor(private router: Router) {}

  ngOnInit(): void {
    this.router.navigate(['/forgot-password']);
  }
}
