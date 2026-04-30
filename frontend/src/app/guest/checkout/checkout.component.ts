import { Component, inject, OnInit, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, Router } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { BookingApiService } from '../../core/services/booking-api.service';
import { ToastService } from '../../core/services/toast.service';
import { CartService } from '../../core/services/cart.service';
import { Cart } from '../../core/models/booking.models';

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
  private router = inject(Router);

  cart = signal<Cart | null>(null);
  isLoading = signal(true);
  error = signal<string | null>(null);
  placing = signal(false);
  checkoutStep = signal<1 | 2 | 3>(1);
  confirmedBookingId = signal<string | null>(null);

  paymentOpen = signal(false);
  paymentStep = signal<'checkout' | 'processing' | 'otp' | 'result'>('checkout');
  paymentError = signal<string | null>(null);
  confirming = signal(false);

  activeBookingId = signal<string | null>(null);
  rzpOrderId = signal<string | null>(null);
  rzpPaymentId = signal<string | null>(null);
  rzpSignature = signal<string | null>(null);
  rzpResultSuccess = signal<boolean | null>(null);
  rzpResultMessage = signal<string | null>(null);
  rzpAmount = signal<number>(0);

  rzpMethod: 'upi' | 'card' = 'upi';
  rzpUpiVpa = '';
  rzpCardNumber = '';
  rzpCardName = '';
  rzpCardExpiry = '';
  rzpCardCvv = '';
  rzpOtp = '';

  promoCode = '';
  promoError = signal<string | null>(null);

  total = computed(() => (this.cart()?.items ?? []).reduce((sum, item) => sum + this.subtotal(item), 0));
  taxes = computed(() => Math.round(this.total() * 0.09));
  discount = computed(() => this.cartService.promoApplied() ? Math.round(this.total() * 0.15) : 0);
  payableTotal = computed(() => this.total() + this.taxes() - this.discount());

  ngOnInit(): void {
    this.promoCode = this.cartService.promoCode();
    this.bookingApi.getCart().subscribe({
      next: (c) => { this.cart.set(c); this.cartService.sync(c); this.isLoading.set(false); },
      error: () => { this.error.set('Failed to load cart.'); this.isLoading.set(false); }
    });
  }

  placeOrder(): void {
    if (this.placing() || this.paymentOpen()) return;

    this.placing.set(true);
    const code = this.cartService.promoApplied() ? this.cartService.promoCode() : undefined;
    this.bookingApi.checkout(code).subscribe({
      next: (res) => {
        this.placing.set(false);
        this.openDummyRazorpay(res.bookingId, this.payableTotal());
      },
      error: (err) => {
        this.placing.set(false);
        this.toast.error(err.error?.message ?? 'Checkout failed.');
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

  closePayment(): void {
    if (this.paymentStep() === 'processing' || this.confirming()) return;
    this.paymentOpen.set(false);
    this.paymentStep.set('checkout');
    this.paymentError.set(null);

    this.activeBookingId.set(null);
    this.rzpOrderId.set(null);
    this.rzpPaymentId.set(null);
    this.rzpSignature.set(null);
    this.rzpResultSuccess.set(null);
    this.rzpResultMessage.set(null);
    this.rzpAmount.set(0);

    this.rzpMethod = 'upi';
    this.rzpUpiVpa = '';
    this.rzpCardNumber = '';
    this.rzpCardName = '';
    this.rzpCardExpiry = '';
    this.rzpCardCvv = '';
    this.rzpOtp = '';
  }

  private openDummyRazorpay(bookingId: string, amount: number): void {
    this.activeBookingId.set(bookingId);
    this.rzpAmount.set(amount);

    this.rzpOrderId.set(`order_${this.randomId(14)}`);
    this.rzpPaymentId.set(null);
    this.rzpSignature.set(null);
    this.rzpResultSuccess.set(null);
    this.rzpResultMessage.set(null);
    this.paymentError.set(null);
    this.paymentStep.set('checkout');
    this.paymentOpen.set(true);
  }

  submitPayment(): void {
    if (this.paymentStep() !== 'checkout') return;

    const validationError = this.validatePaymentInputs();
    if (validationError)
    {
      this.paymentError.set(validationError);
      return;
    }

    this.paymentError.set(null);
    this.paymentStep.set('processing');

    const delayMs = 1200 + Math.floor(Math.random() * 1200);
    window.setTimeout(() => {
      if (this.rzpMethod === 'upi')
      {
        const outcome = this.evaluateUpiOutcome(this.rzpUpiVpa);
        this.finalizeDummyPayment(outcome);
        return;
      }

      // Card payments: show an OTP step for realism
      this.rzpPaymentId.set(`pay_${this.randomId(14)}`);
      this.rzpOtp = '';
      this.paymentStep.set('otp');
    }, delayMs);
  }

  submitOtp(): void {
    if (this.paymentStep() !== 'otp') return;

    const otp = (this.rzpOtp ?? '').replace(/\D/g, '');
    if (otp.length !== 6)
    {
      this.paymentError.set('Enter a valid 6-digit OTP.');
      return;
    }

    this.paymentError.set(null);
    this.paymentStep.set('processing');

    const delayMs = 900 + Math.floor(Math.random() * 900);
    window.setTimeout(() => {
      const outcome = this.evaluateCardOutcome(this.rzpCardNumber);
      this.finalizeDummyPayment(outcome);
    }, delayMs);
  }

  continueAfterPayment(): void {
    if (this.paymentStep() !== 'result' || this.confirming()) return;

    const bookingId = this.activeBookingId();
    const success = this.rzpResultSuccess() === true;

    if (!bookingId)
    {
      this.toast.error('Missing booking id.');
      this.closePayment();
      return;
    }

    this.confirming.set(true);
    this.bookingApi.simulatePayment(bookingId, success).subscribe({
      next: () => {
        this.confirming.set(false);
        this.paymentOpen.set(false);

        if (success)
        {
          this.toast.success('Payment successful! Booking confirmed.');
          this.cartService.sync({ ...(this.cart()!), items: [] });
          this.confirmedBookingId.set(bookingId);
          this.checkoutStep.set(3);
        }
        else
        {
          this.toast.error('Payment failed.');
          this.router.navigate(['/bookings']);
        }
      },
      error: (err) => {
        this.confirming.set(false);
        this.toast.error(err.error?.message ?? 'Failed to confirm payment.');
      }
    });
  }

  private validatePaymentInputs(): string | null {
    if (this.rzpMethod === 'upi')
    {
      const vpa = (this.rzpUpiVpa ?? '').trim();
      if (!vpa) return 'Enter a UPI ID.';

      // Basic VPA validation (close to common UPI formats)
      const vpaRegex = /^[a-zA-Z0-9._-]{2,256}@[a-zA-Z]{2,64}$/;
      if (!vpaRegex.test(vpa)) return 'Enter a valid UPI ID (e.g., name@bank).';
      return null;
    }

    const digits = (this.rzpCardNumber ?? '').replace(/\D/g, '');
    if (digits.length !== 16) return 'Enter a valid 16-digit card number.';
    if (!this.passesLuhn(digits)) return 'Card number is invalid.';

    const name = (this.rzpCardName ?? '').trim();
    if (!name) return 'Enter the cardholder name.';
    if (name.length < 2) return 'Enter a valid cardholder name.';

    const exp = (this.rzpCardExpiry ?? '').trim();
    if (!/^\d{2}\/\d{2}$/.test(exp)) return 'Enter expiry as MM/YY.';
    const mm = Number(exp.slice(0, 2));
    if (mm < 1 || mm > 12) return 'Enter a valid expiry month.';

    // Expiry must not be in the past
    const yy = Number(exp.slice(3, 5));
    const now = new Date();
    const expYear = 2000 + yy;
    const expMonthIndex = mm - 1;
    const endOfExpMonth = new Date(expYear, expMonthIndex + 1, 0, 23, 59, 59, 999);
    if (endOfExpMonth.getTime() < now.getTime()) return 'Card has expired.';

    const cvv = (this.rzpCardCvv ?? '').replace(/\D/g, '');
    if (cvv.length !== 3) return 'Enter a valid 3-digit CVV.';

    return null;
  }

  private evaluateUpiOutcome(vpaRaw: string): { success: boolean; message: string } {
    const vpa = (vpaRaw ?? '').trim().toLowerCase();
    const [user, handle] = vpa.split('@');

    if ((user ?? '').includes('fail'))
      return { success: false, message: 'UPI payment declined by bank.' };

    const allowedHandles = new Set([
      'okaxis',
      'okicici',
      'oksbi',
      'okhdfcbank',
      'ybl',
      'ibl',
      'paytm',
      'upi'
    ]);

    if (!handle || !allowedHandles.has(handle))
      return { success: false, message: 'UPI handle not supported in sandbox.' };

    return { success: true, message: 'Payment successful. UPI payment captured.' };
  }

  private evaluateCardOutcome(cardNumberRaw: string): { success: boolean; message: string } {
    const digits = (cardNumberRaw ?? '').replace(/\D/g, '');

    // Deterministic dummy decline rules (based on card number)
    if (digits.endsWith('0000'))
      return { success: false, message: 'Payment failed: Insufficient funds.' };

    if (digits.endsWith('2222'))
      return { success: false, message: 'Payment failed: Card declined by issuer.' };

    if (digits.endsWith('3333'))
      return { success: false, message: 'Payment failed: Transaction not permitted.' };

    return { success: true, message: 'Payment successful. Card payment captured.' };
  }

  private finalizeDummyPayment(outcome: { success: boolean; message: string }): void {
    const orderId = this.rzpOrderId() ?? `order_${this.randomId(14)}`;
    this.rzpOrderId.set(orderId);
    this.rzpResultSuccess.set(outcome.success);
    this.rzpResultMessage.set(outcome.message);

    if (outcome.success)
    {
      const paymentId = this.rzpPaymentId() ?? `pay_${this.randomId(14)}`;
      const signature = `sig_${this.randomId(20)}`;
      this.rzpPaymentId.set(paymentId);
      this.rzpSignature.set(signature);
    }
    else
    {
      this.rzpSignature.set(null);
    }

    this.paymentStep.set('result');
  }

  private passesLuhn(digits: string): boolean {
    let sum = 0;
    let shouldDouble = false;

    for (let i = digits.length - 1; i >= 0; i--)
    {
      let n = digits.charCodeAt(i) - 48;
      if (n < 0 || n > 9) return false;

      if (shouldDouble)
      {
        n *= 2;
        if (n > 9) n -= 9;
      }
      sum += n;
      shouldDouble = !shouldDouble;
    }
    return sum % 10 === 0;
  }

  private randomId(length: number): string {
    const alphabet = 'abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789';
    const bytes = new Uint8Array(length);

    const cryptoObj = (globalThis as any).crypto as Crypto | undefined;
    if (cryptoObj?.getRandomValues)
    {
      cryptoObj.getRandomValues(bytes);
    }
    else
    {
      for (let i = 0; i < bytes.length; i++) bytes[i] = Math.floor(Math.random() * 256);
    }

    let out = '';
    for (let i = 0; i < bytes.length; i++)
    {
      out += alphabet[bytes[i] % alphabet.length];
    }
    return out;
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
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const link = document.createElement('a');
        link.href = url;
        link.download = `Invoice_${bookingId.substring(0, 8)}.pdf`;
        link.click();
        window.URL.revokeObjectURL(url);
      },
      error: () => this.toast.error('Failed to download invoice.')
    });
  }
}
