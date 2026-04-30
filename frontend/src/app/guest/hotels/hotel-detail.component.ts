import { AfterViewInit, Component, ElementRef, HostListener, OnDestroy, ViewChild, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterLink, ActivatedRoute } from '@angular/router';
import { ReactiveFormsModule, FormBuilder, Validators } from '@angular/forms';
import { HotelApiService } from '../../core/services/hotel-api.service';
import { BookingApiService } from '../../core/services/booking-api.service';
import { ToastService } from '../../core/services/toast.service';
import { CartService } from '../../core/services/cart.service';
import { Hotel, RoomType } from '../../core/models/hotel.models';
import { AuthService } from '../../core/services/auth.service';
import { Router } from '@angular/router';

@Component({
  selector: 'app-hotel-detail',
  standalone: true,
  imports: [CommonModule, RouterLink, ReactiveFormsModule],
  templateUrl: './hotel-detail.component.html',
  styleUrl: './hotel-detail.component.css'
})
export class HotelDetailComponent implements OnInit, AfterViewInit, OnDestroy {
  @ViewChild('bookingPanel') bookingPanel?: ElementRef<HTMLElement>;

  private route = inject(ActivatedRoute);
  private router = inject(Router);
  private hotelApi = inject(HotelApiService);
  private bookingApi = inject(BookingApiService);
  private toast = inject(ToastService);
  private cartService = inject(CartService);
  private authService = inject(AuthService);
  private fb = inject(FormBuilder);

  hotel = signal<Hotel | null>(null);
  isLoading = signal(true);
  error = signal<string | null>(null);
  selectedRoom = signal<RoomType | null>(null);
  addingToCart = signal(false);
  addedToCart = signal(false);
  formError = signal<string | null>(null);
  viewingNow = signal(0);
  viewingPulse = signal(false);
  showStickyPriceBar = signal(false);
  private viewingTimer?: ReturnType<typeof setInterval>;
  private stickyObserver?: IntersectionObserver;

  today = new Date().toISOString().split('T')[0];

  cartForm = this.fb.group({
    checkInDate:  ['', Validators.required],
    checkOutDate: ['', Validators.required],
    guests:       [1, [Validators.required, Validators.min(1)]]
  });

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    const checkIn = this.route.snapshot.queryParamMap.get('checkIn');
    const checkOut = this.route.snapshot.queryParamMap.get('checkOut');
    if (checkIn || checkOut) {
      this.cartForm.patchValue({
        checkInDate: checkIn ?? '',
        checkOutDate: checkOut ?? ''
      });
    }
    this.hotelApi.getHotelById(id).subscribe({
      next: (h) => {
        this.hotel.set(h);
        this.selectedRoom.set(h.roomTypes?.find(room => room.availableRooms > 0 && room.status !== 'Inactive') ?? h.roomTypes?.[0] ?? null);
        this.viewingNow.set(this.hotelHash() % 7 + 3);
        this.isLoading.set(false);
        setTimeout(() => this.initStickyObserver(), 0);
      },
      error: () => { this.error.set('Hotel not found.'); this.isLoading.set(false); }
    });
  }

  selectRoom(room: RoomType): void {
    if (room.availableRooms <= 0 || room.status === 'Inactive') {
      this.toast.error('This room type is not available.');
      return;
    }
    this.selectedRoom.set(room);
    this.formError.set(null);
    this.cartForm.patchValue({ guests: Math.min(this.cartForm.value.guests || 1, room.maxGuests || 1) });
  }

  changeGuests(delta: number): void {
    const room = this.selectedRoom();
    const max = room?.maxGuests || 1;
    const next = Math.max(1, Math.min(max, (this.cartForm.value.guests || 1) + delta));
    this.cartForm.patchValue({ guests: next });
  }

  addToCart(): void {
    if (!this.authService.getCurrentUser()) {
      this.router.navigate(['/login'], { queryParams: { returnUrl: this.router.url } });
      return;
    }
    this.formError.set(null);
    if (!this.selectedRoom()) {
      this.formError.set('Select a room first.');
      return;
    }
    if (this.cartForm.invalid) { this.cartForm.markAllAsTouched(); return; }
    const { checkInDate, checkOutDate, guests } = this.cartForm.value;
    if (checkInDate! < this.today) {
      this.formError.set('Check-in date cannot be in the past.');
      return;
    }
    if (checkInDate! >= checkOutDate!) {
      this.formError.set('Check-out must be after check-in.');
      return;
    }
    if (guests! > (this.selectedRoom()?.maxGuests ?? guests!)) {
      this.toast.error(`This room allows up to ${this.selectedRoom()?.maxGuests} guest(s).`);
      return;
    }
    this.addingToCart.set(true);
    this.bookingApi.addToCart({
      hotelId: this.hotel()!.hotelId,
      roomTypeId: this.selectedRoom()!.roomTypeId,
      checkInDate: checkInDate!,
      checkOutDate: checkOutDate!,
      guests: guests!
    }).subscribe({
      next: () => {
        this.addingToCart.set(false);
        this.addedToCart.set(true);
        this.toast.success('Added to cart!');
        this.cartService.increment();
        setTimeout(() => this.addedToCart.set(false), 1800);
      },
      error: (err) => {
        this.addingToCart.set(false);
        this.toast.error(err.error?.message ?? 'Failed to add to cart.');
      }
    });
  }

  ngAfterViewInit(): void {
    this.viewingTimer = setInterval(() => {
      const delta = this.hotelHash() % 2 === 0 ? 1 : -1;
      this.viewingNow.set(Math.max(3, this.viewingNow() + delta));
      this.viewingPulse.set(true);
      setTimeout(() => this.viewingPulse.set(false), 800);
    }, 30000);
  }

  ngOnDestroy(): void {
    if (this.viewingTimer) clearInterval(this.viewingTimer);
    this.stickyObserver?.disconnect();
  }

  @HostListener('window:scroll')
  onScroll(): void {
    this.updateStickyPriceBar();
  }

  @HostListener('window:resize')
  onResize(): void {
    this.updateStickyPriceBar();
  }

  nights(): number {
    const { checkInDate, checkOutDate } = this.cartForm.value;
    if (!checkInDate || !checkOutDate || checkInDate >= checkOutDate) return 0;
    return Math.max(1, Math.round((new Date(checkOutDate).getTime() - new Date(checkInDate).getTime()) / 86400000));
  }

  roomSubtotal(): number {
    return this.nights() * (this.selectedRoom()?.pricePerNight ?? 0);
  }

  taxes(): number {
    return Math.round(this.roomSubtotal() * 0.09);
  }

  total(): number {
    return this.roomSubtotal() + this.taxes();
  }

  minPrice(): number {
    const rooms = this.hotel()?.roomTypes?.filter(room => room.availableRooms > 0 && room.status !== 'Inactive') ?? [];
    return rooms.length ? Math.min(...rooms.map(room => room.pricePerNight)) : (this.selectedRoom()?.pricePerNight ?? 0);
  }

  lowAvailability(): number | null {
    const rooms = this.hotel()?.roomTypes ?? [];
    const available = rooms.filter(room => room.availableRooms > 0 && room.status !== 'Inactive').map(room => room.availableRooms);
    if (!available.length) return null;
    const min = Math.min(...available);
    return min <= 3 ? min : null;
  }

  roomLowAvailability(room: RoomType): boolean {
    return room.availableRooms > 0 && room.availableRooms <= 3;
  }

  lastBookedHours(): number {
    return this.hotelHash() % 6 + 1;
  }

  scrollToBooking(): void {
    this.bookingPanel?.nativeElement.scrollIntoView({ behavior: 'smooth', block: 'center' });
  }

  gradientClass(): string {
    const h = this.hotel();
    const text = `${h?.name} ${h?.city} ${h?.description}`.toLowerCase();
    if (text.includes('goa') || text.includes('beach')) return 'gradient-beach';
    if (text.includes('manali') || text.includes('mountain')) return 'gradient-mountain';
    if (text.includes('palace') || text.includes('luxury') || text.includes('jaipur') || text.includes('udaipur')) return 'gradient-palace';
    if (text.includes('city') || text.includes('business') || text.includes('mumbai') || text.includes('delhi') || text.includes('bangalore')) return 'gradient-city';
    return 'gradient-default';
  }

  hotelInitials(): string {
    const name = this.hotel()?.name ?? 'SE';
    return name.split(' ').map(part => part[0]).join('').slice(0, 2).toUpperCase();
  }

  rating(): number {
    return Math.max(1, Math.min(5, Math.round(this.hotel()?.starRating || 4)));
  }

  amenities(): string[] {
    const h = this.hotel();
    const text = `${h?.description} ${h?.name}`.toLowerCase();
    const amenities = ['📶 Free WiFi', '🅿️ Parking', '❄️ Air Conditioning', '⏰ 24hr Front Desk'];
    if (text.includes('pool')) amenities.unshift('🏊 Swimming Pool');
    if (text.includes('beach')) amenities.unshift('🏖️ Beach Access');
    if (text.includes('mountain')) amenities.unshift('🏔️ Mountain View');
    if (text.includes('restaurant')) amenities.unshift('🍽️ Restaurant');
    return [...new Set(amenities)];
  }

  private hotelHash(): number {
    const h = this.hotel();
    return Array.from(h?.hotelId || h?.name || 'stayeasy').reduce((sum, char) => sum + char.charCodeAt(0), 0);
  }

  private updateStickyPriceBar(): void {
    const el = this.bookingPanel?.nativeElement;
    if (!el) return;
    const rect = el.getBoundingClientRect();
    this.showStickyPriceBar.set(rect.bottom < 0 || rect.top > window.innerHeight);
  }

  private initStickyObserver(): void {
    const el = this.bookingPanel?.nativeElement;
    if (!el || !('IntersectionObserver' in window)) {
      this.updateStickyPriceBar();
      return;
    }
    this.stickyObserver?.disconnect();
    this.stickyObserver = new IntersectionObserver(([entry]) => {
      const rect = entry.boundingClientRect;
      this.showStickyPriceBar.set(!entry.isIntersecting && (rect.bottom < 0 || window.scrollY > 240));
    }, { threshold: 0.05 });
    this.stickyObserver.observe(el);
    this.updateStickyPriceBar();
  }
}
