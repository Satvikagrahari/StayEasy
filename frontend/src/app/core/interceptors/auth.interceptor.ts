import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const toast = inject(ToastService);
  const token = authService.getToken();

  const authReq = token
    ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } })
    : req;

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        authService.clearAuth();
        toast.error('Session expired. Please log in.');
        router.navigate(['/login']);
      } else if (error.status === 500) {
        toast.error('Server error. Please try again.');
      } else if (error.status === 0) {
        toast.error('Network error. Check your connection.');
      } else if (error.status >= 400) {
        toast.error(error.error?.message ?? 'Request failed. Please try again.');
      }
      return throwError(() => error);
    })
  );
};
