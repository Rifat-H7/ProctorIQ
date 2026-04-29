import { Routes } from '@angular/router';
import { LoginPageComponent } from './features/auth/login-page.component';
import { RegisterPageComponent } from './features/auth/register-page.component';
import { AdminHomeComponent } from './features/admin/admin-home.component';
import { ProctorHomeComponent } from './features/proctor/proctor-home.component';
import { CandidateHomeComponent } from './features/candidate/candidate-home.component';
import { authGuard } from './core/auth/auth.guard';
import { roleGuard } from './core/auth/role.guard';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: LoginPageComponent },
  { path: 'register', component: RegisterPageComponent },
  { path: 'admin', canActivate: [authGuard, roleGuard('Admin')], component: AdminHomeComponent },
  { path: 'proctor', canActivate: [authGuard, roleGuard('Proctor')], component: ProctorHomeComponent },
  { path: 'candidate', canActivate: [authGuard, roleGuard('Candidate')], component: CandidateHomeComponent },
  { path: '**', redirectTo: 'login' }
];
