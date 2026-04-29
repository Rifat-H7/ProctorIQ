import { Component } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-admin-home',
  standalone: true,
  template: `<section class="dash"><h1>Admin Dashboard</h1><p>Manage exams and publishing.</p><button (click)="logout()">Logout</button></section>`,
  styles: [`.dash { padding: 24px; }`]
})
export class AdminHomeComponent {
  constructor(private readonly auth: AuthService) {}
  logout() { this.auth.logout(); }
}
