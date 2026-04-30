import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { ToastService } from '../services/toast.service';

const isAdmin = (role?: string | null): boolean => role?.toLowerCase() === 'admin';

export const adminGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);

  if (isAdmin(authService.getCurrentUser()?.role)) {
    return true;
  }

  return router.createUrlTree(['/hotels']);
};

export const nonAdminGuard: CanActivateFn = (route, state) => {
  const authService = inject(AuthService);
  const router = inject(Router);
  const toast = inject(ToastService);

  if (isAdmin(authService.getCurrentUser()?.role)) {
    toast.warning('Admins cannot access the shopping cart.');
    return router.createUrlTree(['/admin/dashboard']);
  }

  return true;
};

export const roleGuard: CanActivateFn = (route, state) => {
  return adminGuard(route, state);
};
