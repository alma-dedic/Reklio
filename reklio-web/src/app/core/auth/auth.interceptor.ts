import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, switchMap, throwError } from 'rxjs';
import { AuthService } from './auth.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);

  // Ne diraj auth endpointe (login/refresh/logout) — bez tokena i bez refresh-petlje.
  const isAuthCall = req.url.includes('/auth/');

  const token = auth.getToken();
  const authReq =
    token && !isAuthCall
      ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
      : req;

  return next(authReq).pipe(
    catchError((err: HttpErrorResponse) => {
      // Samo zaštićeni poziv s isteklim access tokenom → pokušaj refresh pa retry.
      if (err.status !== 401 || isAuthCall || !auth.getRefreshToken()) {
        return throwError(() => err);
      }

      return auth.refresh().pipe(
        switchMap((r) =>
          next(req.clone({ setHeaders: { Authorization: `Bearer ${r.accessToken}` } })),
        ),
        catchError((refreshErr) => {
          // Refresh pao (istekao/nevažeći) → odjava + na login.
          auth.logout();
          router.navigate(['/login']);
          return throwError(() => refreshErr);
        }),
      );
    }),
  );
};
