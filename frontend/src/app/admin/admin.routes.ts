import { Routes } from '@angular/router';

export const ADMIN_ROUTES: Routes = [
  { path: 'dashboard', title: 'Dashboard', data: { title: 'Dashboard' }, loadComponent: () => import('./dashboard/dashboard.component').then(m => m.DashboardComponent) },
  { path: 'hotels', title: 'Hotels', data: { title: 'Hotels' }, loadComponent: () => import('./hotels/hotels.component').then(m => m.HotelsComponent) },
  { path: 'manage-rooms', title: 'Rooms', data: { title: 'Rooms' }, loadComponent: () => import('./manage-rooms/manage-rooms.component').then(m => m.ManageRoomsComponent) },
  { path: 'manage-bookings', title: 'Bookings', data: { title: 'Bookings' }, loadComponent: () => import('./manage-bookings/manage-bookings.component').then(m => m.ManageBookingsComponent) },
  { path: 'reports', title: 'Reports', data: { title: 'Reports' }, loadComponent: () => import('./reports/reports.component').then(m => m.ReportsComponent) },
  { path: 'users', title: 'Users', data: { title: 'Users' }, loadComponent: () => import('./users/users.component').then(m => m.UsersComponent) },
  { path: '', redirectTo: 'dashboard', pathMatch: 'full' }
];
