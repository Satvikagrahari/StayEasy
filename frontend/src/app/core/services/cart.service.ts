import { Injectable, inject, signal } from '@angular/core';
import { BehaviorSubject, EMPTY, catchError } from 'rxjs';
import { AuthService } from './auth.service';
import { BookingApiService } from './booking-api.service';
import { Cart } from '../models/booking.models';

@Injectable({ providedIn: 'root' })
export class CartService {
  private authService = inject(AuthService);
  private bookingApi = inject(BookingApiService);
  private countSubject = new BehaviorSubject<number>(0);
  private pulseSubject = new BehaviorSubject<number>(0);

  count$ = this.countSubject.asObservable();
  pulse$ = this.pulseSubject.asObservable();

  promoApplied = signal(false);
  promoCode = signal('');

  constructor() {
    this.authService.currentUser$.subscribe(user => {
      if (user && user.role?.toLowerCase() !== 'admin') {
        this.refresh();
      } else {
        this.setCount(0);
      }
    });
  }

  refresh(): void {
    if (!this.authService.getCurrentUser()) {
      this.setCount(0);
      return;
    }

    this.bookingApi.getCart().pipe(
      catchError(() => {
        this.setCount(0);
        return EMPTY;
      })
    ).subscribe(cart => this.sync(cart));
  }

  sync(cart: Cart | null): void {
    this.setCount(cart?.items?.length ?? 0);
  }

  increment(): void {
    this.setCount(this.countSubject.value + 1, true);
  }

  private setCount(count: number, forcePulse = false): void {
    const previous = this.countSubject.value;
    this.countSubject.next(count);
    if (forcePulse || count > previous) {
      this.pulseSubject.next(Date.now());
    }
  }

  applyPromo(code: string): boolean {
    if (code.trim().toUpperCase() === 'STAYEASY15') {
      this.promoApplied.set(true);
      this.promoCode.set(code.trim().toUpperCase());
      return true;
    }
    this.promoApplied.set(false);
    this.promoCode.set('');
    return false;
  }

  resetPromo(): void {
    this.promoApplied.set(false);
    this.promoCode.set('');
  }
}
