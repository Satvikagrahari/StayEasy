import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { BookingApiService } from '../../core/services/booking-api.service';
import { Booking } from '../../core/models/booking.models';

@Component({
  selector: 'app-booking-confirmation',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './booking-confirmation.component.html',
  styleUrl: './booking-confirmation.component.css'
})
export class BookingConfirmationComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private bookingApi = inject(BookingApiService);

  booking = signal<Booking | null>(null);
  isLoading = signal(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
    const bookingId = this.route.snapshot.paramMap.get('bookingId')!;
    this.bookingApi.getMyBookings().subscribe({
      next: (bookings) => {
        const found = bookings.find(b => b.id === bookingId) ?? null;
        this.booking.set(found);
        if (!found) this.error.set('Booking not found.');
        this.isLoading.set(false);
      },
      error: () => { this.error.set('Failed to load booking.'); this.isLoading.set(false); }
    });
  }

  downloadInvoice(): void {
    const b = this.booking();
    if (!b) return;

    this.bookingApi.downloadInvoice(b.id).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `Invoice_${b.id.substring(0, 8)}.pdf`;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error: (err) => {
        console.error('Download failed', err);
        alert('Failed to download invoice. Please try again later.');
      }
    });
  }
}
