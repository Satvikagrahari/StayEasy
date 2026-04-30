import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink } from '@angular/router';
import { BookingApiService } from '../../core/services/booking-api.service';
import { ToastService } from '../../core/services/toast.service';
import { Booking } from '../../core/models/booking.models';
import { HotelApiService } from '../../core/services/hotel-api.service';
import { Hotel, RoomType } from '../../core/models/hotel.models';

@Component({
  selector: 'app-bookings',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './bookings.component.html',
  styleUrl: './bookings.component.css'
})
export class BookingsComponent implements OnInit {
  private bookingApi = inject(BookingApiService);
  private toast = inject(ToastService);
  private hotelApi = inject(HotelApiService);

  bookings = signal<Booking[]>([]);
  hotels = signal<Record<string, Hotel>>({});
  rooms = signal<Record<string, RoomType>>({});
  isLoading = signal(true);
  error = signal<string | null>(null);
  actionId = signal<string | null>(null);
  activeTab = signal<'upcoming' | 'past' | 'cancelled'>('upcoming');
  cancelTarget = signal<Booking | null>(null);

  ngOnInit(): void { this.load(); }

  load(): void {
    this.isLoading.set(true);
    this.bookingApi.getMyBookings().subscribe({
      next: (data) => { this.bookings.set(data); this.loadDetails(data); this.isLoading.set(false); },
      error: () => { this.error.set('Failed to load bookings.'); this.isLoading.set(false); }
    });
  }

  cancel(id: string): void {
    this.actionId.set(id);
    this.bookingApi.cancelBooking(id).subscribe({
      next: () => { this.actionId.set(null); this.toast.success('Booking cancelled.'); this.load(); },
      error: (err) => { this.actionId.set(null); this.toast.error(err.error?.message ?? 'Failed to cancel.'); }
    });
  }

  confirmCancel(): void {
    const target = this.cancelTarget();
    if (!target) return;
    this.cancelTarget.set(null);
    this.cancel(target.id);
  }

  writeReview(): void {
    this.toast.info('Reviews coming soon!');
  }

  filteredBookings(): Booking[] {
    const now = new Date();
    return this.bookings().filter(booking => {
      const status = booking.status.toLowerCase();
      const lastDate = booking.bookingItems?.length
        ? new Date(Math.max(...booking.bookingItems.map(item => new Date(item.checkOutDate).getTime())))
        : new Date(booking.bookingDate);
      if (this.activeTab() === 'cancelled') return ['cancelled', 'refunded', 'refundrequested'].includes(status);
      if (this.activeTab() === 'past') return !['cancelled', 'refunded', 'refundrequested'].includes(status) && lastDate < now;
      return !['cancelled', 'refunded', 'refundrequested'].includes(status) && lastDate >= now;
    });
  }

  firstItem(booking: Booking) {
    return booking.bookingItems?.[0];
  }

  hotelName(booking: Booking): string {
    const item = this.firstItem(booking);
    return item ? this.hotels()[item.hotelId]?.name ?? `Hotel ${item.hotelId.slice(0, 8)}` : 'StayEasy Hotel';
  }

  roomName(booking: Booking): string {
    const item = this.firstItem(booking);
    return item ? this.rooms()[item.roomTypeId]?.name ?? `Room ${item.roomTypeId.slice(0, 8)}` : 'Room';
  }

  location(booking: Booking): string {
    const item = this.firstItem(booking);
    const hotel = item ? this.hotels()[item.hotelId] : null;
    return hotel ? `${hotel.city}, ${hotel.country || 'India'}` : 'India';
  }

  nights(booking: Booking): number {
    return booking.bookingItems?.reduce((sum, item) => sum + (item.nights || this.diffNights(item.checkInDate, item.checkOutDate)), 0) ?? 0;
  }

  dateRange(booking: Booking): string {
    const item = this.firstItem(booking);
    if (!item) return 'Dates unavailable';
    return `${new Date(item.checkInDate).toLocaleDateString('en-IN', { month: 'short', day: 'numeric' })} – ${new Date(item.checkOutDate).toLocaleDateString('en-IN', { month: 'short', day: 'numeric', year: 'numeric' })}`;
  }

  canCancel(booking: Booking): boolean {
    return ['pending', 'confirmed'].includes(booking.status.toLowerCase()) && this.activeTab() === 'upcoming';
  }

  statusClass(status: string): string {
    const s = status.toLowerCase();
    if (s === 'confirmed') return 'status-confirmed';
    if (s === 'pending') return 'status-pending';
    if (s === 'cancelled') return 'status-cancelled';
    return 'status-refunded';
  }

  requestRefund(id: string): void {
    this.actionId.set(id);
    this.bookingApi.requestRefund(id).subscribe({
      next: () => { this.actionId.set(null); this.toast.success('Refund requested.'); this.load(); },
      error: (err) => { this.actionId.set(null); this.toast.error(err.error?.message ?? 'Failed to request refund.'); }
    });
  }

  statusBadge(status: string): string {
    const map: Record<string, string> = {
      Pending: 'badge badge-warning',
      Confirmed: 'badge badge-success',
      Cancelled: 'badge badge-danger',
      RefundRequested: 'badge badge-info',
      Refunded: 'badge badge-info'
    };
    return map[status] ?? 'badge badge-secondary';
  }

  private diffNights(start: string, end: string): number {
    return Math.max(1, Math.round((new Date(end).getTime() - new Date(start).getTime()) / 86400000));
  }

  private loadDetails(bookings: Booking[]): void {
    for (const item of bookings.flatMap(booking => booking.bookingItems ?? [])) {
      if (!this.hotels()[item.hotelId]) {
        this.hotelApi.getHotelById(item.hotelId).subscribe({
          next: hotel => this.hotels.update(map => ({ ...map, [item.hotelId]: hotel })),
          error: () => undefined
        });
      }
      if (!this.rooms()[item.roomTypeId]) {
        this.hotelApi.getRoomTypeById(item.roomTypeId).subscribe({
          next: room => this.rooms.update(map => ({ ...map, [item.roomTypeId]: room })),
          error: () => undefined
        });
      }
    }
  }
}
