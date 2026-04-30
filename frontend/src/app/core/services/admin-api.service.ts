import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import {
  BookingStatusDistribution,
  BookingTrend,
  CancellationReport,
  DashboardSummary,
  OccupancyReport,
  RevenueReport,
  RevenueTrend
} from '../models/admin.models';
import { Booking } from '../models/booking.models';
import { UserProfile } from '../models/auth.models';
import { environment } from '../../../environments/environment';

@Injectable({
  providedIn: 'root'
})
export class AdminApiService {
  private http = inject(HttpClient);
  private readonly BASE = `${environment.gatewayUrl}/gateway/admin`;
  private readonly USERS = `${environment.gatewayUrl}/gateway/admin/users`;

  getDashboardSummary(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.BASE}/dashboard/summary`);
  }

  getDashboardStats(): Observable<DashboardSummary> {
    return this.http.get<DashboardSummary>(`${this.BASE}/dashboard/stats`);
  }

  getBookingTrends(days = 7): Observable<BookingTrend[]> {
    return this.http.get<BookingTrend[]>(`${this.BASE}/bookings/trends`, {
      params: { days: days.toString() }
    });
  }

  getRevenueTrends(months = 12): Observable<RevenueTrend[]> {
    return this.http.get<RevenueTrend[]>(`${this.BASE}/revenue/trends`, {
      params: { months: months.toString() }
    });
  }

  getBookingStatusDistribution(): Observable<BookingStatusDistribution[]> {
    return this.http.get<BookingStatusDistribution[]>(`${this.BASE}/bookings/status-distribution`);
  }

  getRecentBookings(take?: number): Observable<Booking[]> {
    let params = new HttpParams();
    if (take !== undefined) params = params.set('take', take.toString());
    return this.http.get<Booking[]>(`${this.BASE}/bookings/recent`, { params });
  }

  getAllBookings(filters?: { status?: string; startDate?: string; endDate?: string }): Observable<Booking[]> {
    let params = new HttpParams();
    if (filters?.status) params = params.set('status', filters.status);
    if (filters?.startDate) params = params.set('startDate', filters.startDate);
    if (filters?.endDate) params = params.set('endDate', filters.endDate);
    return this.http.get<Booking[]>(`${this.BASE}/bookings`, { params });
  }

  updateBookingStatus(id: string, status: string): Observable<void> {
    return this.http.put<void>(`${this.BASE}/bookings/${id}/status`, null, { params: { status } });
  }

  approveRefund(id: string): Observable<void> {
    return this.http.put<void>(`${this.BASE}/bookings/${id}/refund`, {});
  }

  getOccupancyReport(startDate?: string, endDate?: string): Observable<OccupancyReport[]> {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);
    return this.http.get<OccupancyReport[]>(`${this.BASE}/reports/occupancy`, { params });
  }

  getRevenueReport(startDate?: string, endDate?: string): Observable<RevenueReport[]> {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);
    return this.http.get<RevenueReport[]>(`${this.BASE}/reports/revenue`, { params });
  }

  getCancellationReport(startDate?: string, endDate?: string): Observable<CancellationReport[]> {
    let params = new HttpParams();
    if (startDate) params = params.set('startDate', startDate);
    if (endDate) params = params.set('endDate', endDate);
    return this.http.get<CancellationReport[]>(`${this.BASE}/reports/cancellations`, { params });
  }

  // User management — routes to IdentityService via /gateway/admin/users/
  getAllUsers(): Observable<UserProfile[]> {
    return this.http.get<UserProfile[]>(this.USERS);
  }

  // Backend: PUT /api/admin/users/{id}/status with body { isActive: true/false }
  activateUser(id: string): Observable<void> {
    return this.http.put<void>(`${this.USERS}/${id}/status`, { isActive: true });
  }

  deactivateUser(id: string): Observable<void> {
    return this.http.put<void>(`${this.USERS}/${id}/status`, { isActive: false });
  }

  deleteUser(id: string): Observable<void> {
    return this.http.delete<void>(`${this.USERS}/${id}`);
  }
}
