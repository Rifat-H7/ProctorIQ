import { Component, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { AuthService } from '../../core/auth/auth.service';

@Component({
  selector: 'app-login-page',
  standalone: true,
  imports: [FormsModule, RouterLink],
  templateUrl: './login-page.component.html',
  styleUrl: './auth-pages.component.scss'
})
export class LoginPageComponent {
  email = '';
  password = '';
  error = signal<string | null>(null);

  constructor(private readonly auth: AuthService, private readonly router: Router) {}

  submit() {
    this.error.set(null);
    this.auth.login(this.email, this.password).subscribe({
      next: () => this.redirectByRole(),
      error: () => this.error.set('Login failed. Check credentials.')
    });
  }

  private redirectByRole() {
    const role = this.auth.role();
    if (role === 'Admin') this.router.navigateByUrl('/admin');
    else if (role === 'Proctor') this.router.navigateByUrl('/proctor');
    else this.router.navigateByUrl('/candidate');
  }
}
