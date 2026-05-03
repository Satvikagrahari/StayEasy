import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { environment } from '../../../environments/environment';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { BookingApiService } from '../../core/services/booking-api.service';
import { ToastService } from '../../core/services/toast.service';
import { CartService } from '../../core/services/cart.service';
import { Cart } from '../../core/models/booking.models';
import { AuthService } from '../../core/services/auth.service';

declare var Razorpay: any;

@Component({
  selector: 'app-checkout',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.css'
})
export class CheckoutComponent implements OnInit {
  private bookingApi = inject(BookingApiService);
  private toast = inject(ToastService);
  private cartService = inject(CartService);
  private authService = inject(AuthService);
  private router = inject(Router);

  cart = signal<Cart | null>(null);
  isLoading = signal(true);
  error = signal<string | null>(null);
  placing = signal(false);
  checkoutStep = signal<1 | 2 | 3>(1);
  confirmedBookingId = signal<string | null>(null);

  promoCode = '';
  promoError = signal<string | null>(null);

  total = computed(() => (this.cart()?.items ?? []).reduce((sum: number, item: any) => sum + this.subtotal(item), 0));
  taxes = computed(() => Math.round(this.total() * 0.09));
  discount = computed(() => this.cartService.promoApplied() ? Math.round(this.total() * 0.15) : 0);
  payableTotal = computed(() => this.total() + this.taxes() - this.discount());

  ngOnInit(): void {
    this.promoCode = this.cartService.promoCode();
    this.bookingApi.getCart().subscribe({
      next: (c: Cart) => { this.cart.set(c); this.cartService.sync(c); this.isLoading.set(false); },
      error: () => { this.error.set('Failed to load cart.'); this.isLoading.set(false); }
    });
  }

  placeOrder(): void {
    if (this.placing()) return;

    this.placing.set(true);
    const code = this.cartService.promoApplied() ? this.cartService.promoCode() : undefined;
    this.bookingApi.checkout(code).subscribe({
      next: (res: any) => {
        this.initiateRazorpay(res.bookingId);
      },
      error: () => {
        this.placing.set(false);
      }
    });
  }

  private initiateRazorpay(bookingId: string): void {
    this.bookingApi.createRazorpayOrder(bookingId).subscribe({
      next: (res: any) => {
        const user = this.authService.getCurrentUser();
        
        const options = {
          key: environment.razorpayKey,
          amount: this.payableTotal() * 100,
          currency: 'INR',
          name: 'StayEasy',
          description: 'Hotel Booking Payment',
          order_id: res.orderId,
          handler: (response: any) => {
            this.verifyPayment(response, bookingId);
          },
          prefill: {
            name: user?.username || '',
            email: user?.email || '',
          },
          theme: {
            color: '#0d6efd'
          },
          modal: {
            ondismiss: () => {
              this.placing.set(false);
            }
          }
        };

        const rzp = new Razorpay(options);
        rzp.open();
      },
      error: () => {
        this.placing.set(false);
      }
    });
  }

  private verifyPayment(rzpResponse: any, bookingId: string): void {
    const request = {
      razorpayPaymentId: rzpResponse.razorpay_payment_id,
      razorpayOrderId: rzpResponse.razorpay_order_id,
      razorpaySignature: rzpResponse.razorpay_signature,
      bookingId: bookingId
    };

    this.bookingApi.verifyPayment(request).subscribe({
      next: () => {
        this.placing.set(false);
        this.toast.success('Payment successful! Booking confirmed.');
        this.cartService.sync({ ...(this.cart()!), items: [] });
        this.confirmedBookingId.set(bookingId);
        this.checkoutStep.set(3);
      },
      error: () => {
        this.placing.set(false);
      }
    });
  }

  continueToPayment(): void {
    this.checkoutStep.set(2);
  }

  copyBookingId(): void {
    const id = this.confirmedBookingId();
    if (id && navigator.clipboard) {
      navigator.clipboard.writeText(id);
      this.toast.success('Booking ID copied.');
    }
  }

  nights(item: { checkInDate: string; checkOutDate: string }): number {
    const start = new Date(item.checkInDate).getTime();
    const end = new Date(item.checkOutDate).getTime();
    return Math.max(1, Math.round((end - start) / 86400000));
  }

  subtotal(item: { checkInDate: string; checkOutDate: string; priceSnapshot: number }): number {
    return this.nights(item) * item.priceSnapshot;
  }

  applyPromo(): void {
    if (this.cartService.applyPromo(this.promoCode)) {
      this.promoError.set(null);
      this.toast.success('Promo applied!');
      return;
    }
    this.promoError.set('Invalid promo code.');
  }

  promoApplied() {
    return this.cartService.promoApplied();
  }

  downloadInvoice(): void {
    const bookingId = this.confirmedBookingId();
    if (!bookingId) return;

    this.bookingApi.downloadInvoice(bookingId).subscribe({
      next: (blob: Blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `Invoice_${bookingId.substring(0, 8)}.pdf`;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => undefined
    });
  }
}
