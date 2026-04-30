import { Routes } from '@angular/router';
import { authGuard } from '../core/guards/auth.guard';
import { nonAdminGuard } from '../core/guards/role.guard';

export const GUEST_ROUTES: Routes = [
  { path: 'home', loadComponent: () => import('./home/home.component').then(m => m.HomeComponent) },
  { path: 'catalog', loadComponent: () => import('./catalog/catalog.component').then(m => m.CatalogComponent) },
  { path: 'hotels', loadComponent: () => import('./catalog/catalog.component').then(m => m.CatalogComponent) },
  { path: 'hotels/:id', loadComponent: () => import('./hotels/hotel-detail.component').then(m => m.HotelDetailComponent) },
  { path: 'room-details/:id', loadComponent: () => import('./room-details/room-details.component').then(m => m.RoomDetailsComponent) },
  { path: 'cart', canActivate: [authGuard, nonAdminGuard], loadComponent: () => import('./cart/cart.component').then(m => m.CartComponent) },
  { path: 'checkout', canActivate: [authGuard, nonAdminGuard], loadComponent: () => import('./checkout/checkout.component').then(m => m.CheckoutComponent) },
  { path: 'booking-confirmation/:bookingId', canActivate: [authGuard], loadComponent: () => import('./booking-confirmation/booking-confirmation.component').then(m => m.BookingConfirmationComponent) },
  { path: 'bookings', canActivate: [authGuard, nonAdminGuard], loadComponent: () => import('./bookings/bookings.component').then(m => m.BookingsComponent) },
  { path: 'my-bookings', canActivate: [authGuard, nonAdminGuard], loadComponent: () => import('./bookings/bookings.component').then(m => m.BookingsComponent) },
  { path: '', redirectTo: 'home', pathMatch: 'full' }
];
