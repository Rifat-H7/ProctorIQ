import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../auth/auth.service';
import { Observable, catchError, shareReplay, switchMap, throwError } from 'rxjs';

let refreshInFlight: Observable<unknown> | null = null;

function isAuthEndpoint(url: string): boolean {
  return url.includes('/api/auth/login') || url.includes('/api/auth/register') || url.includes('/api/auth/refresh');
}

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.accessToken();
  const authReq = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;

  return next(authReq).pipe(
    catchError((error) => {
      const isUnauthorized = error?.status === 401;
      if (!isUnauthorized || isAuthEndpoint(req.url)) {
        return throwError(() => error);
      }

      if (!refreshInFlight) {
        refreshInFlight = auth.refresh().pipe(shareReplay(1));
      }

      return refreshInFlight.pipe(
        switchMap(() => {
          refreshInFlight = null;
          const refreshed = auth.accessToken();
          if (!refreshed) {
            auth.logout();
            return throwError(() => error);
          }

          return next(req.clone({ setHeaders: { Authorization: `Bearer ${refreshed}` } }));
        }),
        catchError((refreshError) => {
          refreshInFlight = null;
          auth.logout();
          return throwError(() => refreshError);
        })
      );
    })
  );
};
