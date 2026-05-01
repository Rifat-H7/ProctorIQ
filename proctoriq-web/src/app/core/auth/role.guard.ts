import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService, Role } from './auth.service';

export function roleGuard(expected: Role): CanActivateFn {
  return () => {
    const auth = inject(AuthService);
    const router = inject(Router);

    if (!auth.isAuthenticated()) {
      router.navigateByUrl('/login');
      return false;
    }

    if (auth.role() !== expected) {
      router.navigateByUrl('/');
      return false;
    }

    return true;
  };
}
