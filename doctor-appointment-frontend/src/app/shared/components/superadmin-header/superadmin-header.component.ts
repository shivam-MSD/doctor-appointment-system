import { Component, Input } from '@angular/core';

@Component({
  selector: 'app-superadmin-header',
  templateUrl: './superadmin-header.component.html',
  styleUrls: ['./superadmin-header.component.css']
})
export class SuperadminHeaderComponent {
  @Input() firstName = '';
}
