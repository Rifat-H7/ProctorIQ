import { Component } from '@angular/core';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-candidate-home',
  standalone: true,
  template: `<section class="dash"><h1>Candidate Dashboard</h1><p>Start attempts and track your results.</p><button (click)="logout()">Logout</button></section>`,
  styles: [`.dash { padding: 24px; }`]
})
export class CandidateHomeComponent {
  constructor(private readonly auth: AuthService) {}
  logout() { this.auth.logout(); }
}
