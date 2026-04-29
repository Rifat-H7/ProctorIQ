import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Router } from '@angular/router';
import { environment } from '../../../environments/environment';
import { tap } from 'rxjs';

export type Role = 'Admin' | 'Proctor' | 'Candidate';

interface AuthResponse {
  accessToken: string;
  refreshToken: string;
}

interface JwtPayload {
  role?: string | string[];
  'http://schemas.microsoft.com/ws/2008/06/identity/claims/role'?: string | string[];
  exp?: number;
}

@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly accessTokenState = signal<string | null>(sessionStorage.getItem('pq_access_token'));
  private readonly refreshTokenState = signal<string | null>(sessionStorage.getItem('pq_refresh_token'));

  readonly accessToken = computed(() => this.accessTokenState());
  readonly isAuthenticated = computed(() => !!this.accessTokenState());
  readonly role = computed<Role | null>(() => this.extractRole(this.accessTokenState()));

  constructor(private readonly http: HttpClient, private readonly router: Router) {}

  login(email: string, password: string) {
    return this.http.post<AuthResponse>(`${environment.apiBaseUrl}/api/auth/login`, { email, password }).pipe(
      tap((response) => this.setTokens(response.accessToken, response.refreshToken))
    );
  }

  register(fullName: string, email: string, password: string, role: Role) {
    return this.http.post<AuthResponse>(`${environment.apiBaseUrl}/api/auth/register`, {
      fullName,
      email,
      password,
      role
    }).pipe(tap((response) => this.setTokens(response.accessToken, response.refreshToken)));
  }

  logout() {
    this.accessTokenState.set(null);
    this.refreshTokenState.set(null);
    sessionStorage.removeItem('pq_access_token');
    sessionStorage.removeItem('pq_refresh_token');
    this.router.navigateByUrl('/login');
  }

  private setTokens(accessToken: string, refreshToken: string) {
    this.accessTokenState.set(accessToken);
    this.refreshTokenState.set(refreshToken);
    sessionStorage.setItem('pq_access_token', accessToken);
    sessionStorage.setItem('pq_refresh_token', refreshToken);
  }

  private extractRole(token: string | null): Role | null {
    if (!token) return null;
    try {
      const payloadPart = token.split('.')[1];
      if (!payloadPart) return null;
      const decoded = atob(payloadPart.replace(/-/g, '+').replace(/_/g, '/'));
      const payload = JSON.parse(decoded) as JwtPayload;
      const roleClaim = payload.role ?? payload['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];
      const rawRole = Array.isArray(roleClaim) ? roleClaim[0] : roleClaim;
      if (rawRole === 'Admin' || rawRole === 'Proctor' || rawRole === 'Candidate') {
        return rawRole;
      }
      return null;
    } catch {
      return null;
    }
  }
}
