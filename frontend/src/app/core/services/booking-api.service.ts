import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { Cart, AddToCartRequest, Booking, CheckoutResponse } from '../models/booking.models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class BookingApiService {
  private http = inject(HttpClient);
  private readonly CART = `${environment.gatewayUrl}/gateway/cart`;
  private readonly BOOKING = `${environment.gatewayUrl}/gateway/booking`;

  // Cart
  getCart(): Observable<Cart> {
    return this.http.get<Cart>(this.CART);
  }

  addToCart(request: AddToCartRequest): Observable<string> {
    return this.http.post(`${this.CART}/add`, request, { responseType: 'text' });
  }

  removeFromCart(itemId: string): Observable<string> {
    return this.http.delete(`${this.CART}/${itemId}`, { responseType: 'text' });
  }

  // Booking
  checkout(): Observable<CheckoutResponse> {
    return this.http.post<CheckoutResponse>(`${this.BOOKING}/checkout`, {});
  }

  simulatePayment(bookingId: string, isSuccess: boolean): Observable<void> {
    return this.http.post<void>(`${this.BOOKING}/${bookingId}/simulate-payment`, null, {
      params: { isSuccess: isSuccess.toString() }
    });
  }

  getMyBookings(): Observable<Booking[]> {
    return this.http.get<Booking[]>(this.BOOKING);
  }

  cancelBooking(id: string): Observable<void> {
    return this.http.delete<void>(`${this.BOOKING}/${id}/cancel`);
  }

  requestRefund(id: string): Observable<void> {
    return this.http.post<void>(`${this.BOOKING}/${id}/request-refund`, {});
  }

  // Admin booking operations (via BookingService directly — not used, AdminService handles these)
  getAllBookings(): Observable<Booking[]> {
    return this.http.get<Booking[]>(`${this.BOOKING}/all`);
  }

  getPendingBookings(): Observable<Booking[]> {
    return this.http.get<Booking[]>(`${this.BOOKING}/pending`);
  }

  getConfirmedBookings(): Observable<Booking[]> {
    return this.http.get<Booking[]>(`${this.BOOKING}/confirmed`);
  }

  updateBookingStatus(bookingId: string, status: string): Observable<void> {
    return this.http.put<void>(`${this.BOOKING}/${bookingId}/status`, null, {
      params: { status }
    });
  }

  approveRefund(id: string): Observable<void> {
    return this.http.post<void>(`${this.BOOKING}/${id}/approve-refund`, {});
  }
}
