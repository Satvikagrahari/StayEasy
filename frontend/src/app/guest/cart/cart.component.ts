import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { BookingApiService } from '../../core/services/booking-api.service';
import { ToastService } from '../../core/services/toast.service';
import { CartService } from '../../core/services/cart.service';
import { HotelApiService } from '../../core/services/hotel-api.service';
import { Cart } from '../../core/models/booking.models';
import { Hotel, RoomType } from '../../core/models/hotel.models';

@Component({
  selector: 'app-cart',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './cart.component.html',
  styleUrl: './cart.component.css'
})
export class CartComponent implements OnInit {
  private bookingApi = inject(BookingApiService);
  private toast = inject(ToastService);
  private cartService = inject(CartService);
  private hotelApi = inject(HotelApiService);

  cart = signal<Cart | null>(null);
  isLoading = signal(true);
  error = signal<string | null>(null);
  removing = signal<string | null>(null);
  detailsLoading = signal(false);
  hotels = signal<Record<string, Hotel>>({});
  rooms = signal<Record<string, RoomType>>({});
  promoCode = '';
  promoApplied = signal(false);
  promoError = signal<string | null>(null);

  total = computed(() => {
    const items = this.cart()?.items ?? [];
    return items.reduce((sum, i) => sum + this.subtotal(i), 0);
  });
  taxes = computed(() => Math.round(this.total() * 0.09));
  discount = computed(() => this.promoApplied() ? Math.round(this.total() * 0.15) : 0);
  grandTotal = computed(() => this.total() + this.taxes() - this.discount());

  ngOnInit(): void {
    this.loadCart();
  }

  loadCart(): void {
    this.isLoading.set(true);
    this.bookingApi.getCart().subscribe({
      next: (c) => {
        this.cart.set(c);
        this.cartService.sync(c);
        this.isLoading.set(false);
        this.loadItemDetails(c);
      },
      error: () => { this.error.set('Failed to load cart.'); this.isLoading.set(false); }
    });
  }

  removeItem(itemId: string): void {
    this.removing.set(itemId);
    this.bookingApi.removeFromCart(itemId).subscribe({
      next: () => {
        this.removing.set(null);
        this.toast.success('Item removed.');
        this.loadCart();
      },
      error: (err) => {
        this.removing.set(null);
        this.toast.error(err.error?.message ?? 'Failed to remove item.');
      }
    });
  }

  applyPromo(): void {
    if (this.promoCode.trim().toUpperCase() === 'STAYEASY15') {
      this.promoApplied.set(true);
      this.promoError.set(null);
      return;
    }
    this.promoApplied.set(false);
    this.promoError.set('Invalid promo code.');
  }

  hotelName(item: { hotelId: string }): string {
    return this.hotels()[item.hotelId]?.name ?? `Hotel ${item.hotelId.slice(0, 8)}`;
  }

  roomName(item: { roomTypeId: string }): string {
    return this.rooms()[item.roomTypeId]?.name ?? `Room ${item.roomTypeId.slice(0, 8)}`;
  }

  location(item: { hotelId: string }): string {
    const hotel = this.hotels()[item.hotelId];
    return hotel ? `${hotel.city}, ${hotel.country || 'India'}` : 'India';
  }

  private loadItemDetails(cart: Cart): void {
    const items = cart.items ?? [];
    if (!items.length) return;
    this.detailsLoading.set(true);

    let pending = 0;
    const done = () => {
      pending--;
      if (pending <= 0) this.detailsLoading.set(false);
    };

    for (const item of items) {
      if (!this.hotels()[item.hotelId]) {
        pending++;
        this.hotelApi.getHotelById(item.hotelId).subscribe({
          next: hotel => this.hotels.update(map => ({ ...map, [item.hotelId]: hotel })),
          error: done,
          complete: done
        });
      }
      if (!this.rooms()[item.roomTypeId]) {
        pending++;
        this.hotelApi.getRoomTypeById(item.roomTypeId).subscribe({
          next: room => this.rooms.update(map => ({ ...map, [item.roomTypeId]: room })),
          error: done,
          complete: done
        });
      }
    }

    if (pending === 0) this.detailsLoading.set(false);
  }

  nights(item: { checkInDate: string; checkOutDate: string }): number {
    const start = new Date(item.checkInDate).getTime();
    const end = new Date(item.checkOutDate).getTime();
    return Math.max(1, Math.round((end - start) / 86400000));
  }

  subtotal(item: { checkInDate: string; checkOutDate: string; priceSnapshot: number }): number {
    return this.nights(item) * item.priceSnapshot;
  }
}
