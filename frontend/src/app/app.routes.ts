import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { guestGuard } from './core/guards/guest.guard';
import { adminGuard } from './core/guards/role.guard';

export const routes: Routes = [
  { path: 'login', canActivate: [guestGuard], loadComponent: () => import('./auth/login/login.component').then(m => m.LoginComponent) },
  { path: 'register', canActivate: [guestGuard], loadComponent: () => import('./auth/register/register.component').then(m => m.RegisterComponent) },
  { path: 'verify-otp', canActivate: [guestGuard], loadComponent: () => import('./auth/verify-otp/verify-otp.component').then(m => m.VerifyOtpComponent) },
  { path: 'forgot-password', canActivate: [guestGuard], loadComponent: () => import('./auth/forgot-password/forgot-password.component').then(m => m.ForgotPasswordComponent) },
  { path: 'auth', canActivate: [guestGuard], loadChildren: () => import('./auth/auth.routes').then(m => m.AUTH_ROUTES) },
  { path: 'admin', canActivate: [authGuard, adminGuard], loadChildren: () => import('./admin/admin.routes').then(m => m.ADMIN_ROUTES) },
  { path: 'account-center', canActivate: [authGuard], loadComponent: () => import('./account/account-center.component').then(m => m.AccountCenterComponent) },
  { path: '', loadChildren: () => import('./guest/guest.routes').then(m => m.GUEST_ROUTES) },
  { path: '**', redirectTo: '' }
];
