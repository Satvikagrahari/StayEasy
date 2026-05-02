import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AdminApiService } from '../../core/services/admin-api.service';
import { ToastService } from '../../core/services/toast.service';
import { Booking } from '../../core/models/booking.models';

@Component({
  selector: 'app-manage-bookings',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './manage-bookings.component.html',
  styleUrl: './manage-bookings.component.css'
})
export class ManageBookingsComponent implements OnInit {
  private adminApi = inject(AdminApiService);
  private toast = inject(ToastService);

  bookings = signal<Booking[]>([]);
  isLoading = signal(true);
  error = signal<string | null>(null);
  actionId = signal<string | null>(null);
  selected = signal<Booking | null>(null);
  page = signal(1);

  filterStatus = '';
  filterStart = '';
  filterEnd = '';
  search = '';
  pageSize = 10;

  ngOnInit(): void { this.load(); }

  load(): void {
    this.isLoading.set(true);
    const filters = {
      status: this.filterStatus || undefined,
      startDate: this.filterStart || undefined,
      endDate: this.filterEnd || undefined
    };
    this.adminApi.getAllBookings(filters).subscribe({
      next: (b) => { this.bookings.set(b); this.isLoading.set(false); },
      error: () => { this.error.set('Failed to load bookings.'); this.isLoading.set(false); }
    });
  }

  updateStatus(id: string, status: string): void {
    this.actionId.set(id);
    this.adminApi.updateBookingStatus(id, status).subscribe({
      next: () => { this.actionId.set(null); this.toast.success('Status updated.'); this.load(); },
      error: () => { this.actionId.set(null); }
    });
  }

  approveRefund(id: string): void {
    this.actionId.set(id);
    this.adminApi.approveRefund(id).subscribe({
      next: () => { this.actionId.set(null); this.toast.success('Refund approved.'); this.load(); },
      error: () => { this.actionId.set(null); }
    });
  }

  clear(): void {
    this.filterStatus = '';
    this.filterStart = '';
    this.filterEnd = '';
    this.search = '';
    this.page.set(1);
    this.load();
  }

  filtered(): Booking[] {
    const q = this.search.trim().toLowerCase();
    return this.bookings().filter(b => !q || b.id.toLowerCase().includes(q) || b.userId.toLowerCase().includes(q));
  }

  paged(): Booking[] {
    const start = (this.page() - 1) * this.pageSize;
    return this.filtered().slice(start, start + this.pageSize);
  }

  pages(): number[] {
    return Array.from({ length: Math.max(1, Math.ceil(this.filtered().length / this.pageSize)) }, (_, i) => i + 1);
  }

  nights(booking: Booking): number {
    return booking.bookingItems?.reduce((sum, item) => sum + (item.nights || Math.max(1, Math.round((new Date(item.checkOutDate).getTime() - new Date(item.checkInDate).getTime()) / 86400000))), 0) ?? 0;
  }

  firstItem(booking: Booking) {
    return booking.bookingItems?.[0];
  }

  hotelLabel(booking: Booking): string {
    const item = this.firstItem(booking);
    return item ? `Hotel ${item.hotelId.slice(0, 6)}` : 'Hotel N/A';
  }

  roomLabel(booking: Booking): string {
    const item = this.firstItem(booking);
    return item ? `Room ${item.roomTypeId.slice(0, 6)}` : 'Room N/A';
  }

  checkIn(booking: Booking): string | null {
    return this.firstItem(booking)?.checkInDate ?? null;
  }

  initials(booking: Booking): string {
    return (booking.userId || 'GU').slice(0, 2).toUpperCase();
  }

  guestName(booking: Booking): string {
    return `Guest ${booking.userId.slice(0, 6)}`;
  }

  statusBadge(status: string): string {
    const map: Record<string, string> = {
      Pending: 'badge badge-warning', Confirmed: 'badge badge-success',
      Cancelled: 'badge badge-danger', RefundRequested: 'badge badge-info'
    };
    return map[status] ?? 'badge badge-secondary';
  }
}
