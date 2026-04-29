import { Component } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-proctor-home',
  standalone: true,
  template: `<section class="dash"><h1>Proctor Dashboard</h1><p>Monitor active exam sessions live.</p><button (click)="logout()">Logout</button></section>`,
  styles: [`.dash { padding: 24px; }`]
})
export class ProctorHomeComponent {
  constructor(private readonly auth: AuthService) {}
  logout() { this.auth.logout(); }
}
