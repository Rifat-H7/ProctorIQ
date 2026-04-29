import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService, Role } from '../../core/auth/auth.service';

@Component({
  selector: 'app-register-page',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './register-page.component.html',
  styleUrl: './auth-pages.component.scss'
})
export class RegisterPageComponent {
  fullName = '';
  email = '';
  password = '';
  role: Role = 'Candidate';
  error = signal<string | null>(null);

  constructor(private readonly auth: AuthService, private readonly router: Router) {}

  submit() {
    this.error.set(null);
    this.auth.register(this.fullName, this.email, this.password, this.role).subscribe({
      next: () => this.router.navigateByUrl('/'),
      error: () => this.error.set('Registration failed.')
    });
  }
}
