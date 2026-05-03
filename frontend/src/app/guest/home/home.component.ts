import { Component, OnDestroy, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { HotelApiService } from '../../core/services/hotel-api.service';
import { Hotel } from '../../core/models/hotel.models';
import { AuthService } from '../../core/services/auth.service';
import { ToastService } from '../../core/services/toast.service';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './home.component.html',
  styleUrl: './home.component.css'
})
export class HomeComponent implements OnInit, OnDestroy {
  private hotelApi = inject(HotelApiService);
  private router = inject(Router);
  private auth = inject(AuthService);
  private toast = inject(ToastService);

  featured = signal<Hotel[]>([]);
  isLoading = signal(true);
  error = signal<string | null>(null);
  subscribed = signal(false);
  showPromoModal = signal(false);
  promoCopied = signal(false);
  destinations = ['Goa', 'Jaipur', 'Manali', 'Udaipur', 'Mumbai', 'Delhi', 'Bangalore', 'Jaisalmer'];
  search = { destination: '', checkIn: '', checkOut: '', email: '' };
  readonly today = new Date().toISOString().split('T')[0];
  private promoTimer?: ReturnType<typeof setTimeout>;

  ngOnInit(): void {
    this.hotelApi.getHotels().subscribe({
      next: (hotels) => {
        this.featured.set(hotels.slice(0, 4));
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Failed to load hotels.');
        this.isLoading.set(false);
      }
    });

    const user = this.auth.getCurrentUser();
    if (localStorage.getItem('promo_dismissed_v1') !== 'true' && user?.role?.toLowerCase() !== 'admin') {
      this.promoTimer = setTimeout(() => this.showPromoModal.set(true), 5000);
    }
  }

  ngOnDestroy(): void {
    if (this.promoTimer) clearTimeout(this.promoTimer);
  }

  searchHotels(): void {
    const queryParams: Record<string, string> = {};
    if (this.search.destination.trim()) queryParams['city'] = this.search.destination.trim();
    if (this.search.checkIn) queryParams['checkIn'] = this.search.checkIn;
    if (this.search.checkOut) queryParams['checkOut'] = this.search.checkOut;
    this.router.navigate(['/hotels'], { queryParams });
  }

  goDestination(city: string): void {
    this.router.navigate(['/hotels'], { queryParams: { city } });
  }

  subscribe(): void {
    if (this.search.email.trim()) {
      this.subscribed.set(true);
      this.search.email = '';
    }
  }

  dismissPromoModal(): void {
    localStorage.setItem('promo_dismissed_v1', 'true');
    this.showPromoModal.set(false);
  }

  copyPromo(): void {
    navigator.clipboard?.writeText('STAYEASY15');
    this.promoCopied.set(true);
    this.toast.success('Copied! ✓');
    setTimeout(() => this.promoCopied.set(false), 1500);
  }

  browseFromPromo(): void {
    this.dismissPromoModal();
    this.router.navigate(['/hotels']);
  }

  minPrice(hotel: Hotel): number {
    const rooms = hotel.roomTypes?.filter(room => room.availableRooms > 0 && room.status !== 'Inactive') ?? [];
    return rooms.length ? Math.min(...rooms.map(room => room.pricePerNight)) : 0;
  }

  primaryRoom(hotel: Hotel): string {
    return hotel.roomTypes?.[0]?.name ?? 'Hotel';
  }

  hotelInitials(hotel: Hotel): string {
    return hotel.name.split(' ').map(part => part[0]).join('').slice(0, 2).toUpperCase();
  }

  gradientClass(hotel: Hotel): string {
    const text = `${hotel.name} ${hotel.city} ${hotel.description}`.toLowerCase();
    if (text.includes('goa') || text.includes('beach')) return 'gradient-beach';
    if (text.includes('manali') || text.includes('mountain')) return 'gradient-mountain';
    if (text.includes('palace') || text.includes('luxury') || text.includes('jaipur') || text.includes('udaipur')) return 'gradient-palace';
    if (text.includes('city') || text.includes('business') || text.includes('mumbai') || text.includes('delhi') || text.includes('bangalore')) return 'gradient-city';
    return 'gradient-default';
  }

  rating(hotel: Hotel): number {
    return Math.max(1, Math.min(5, Math.round(hotel.starRating || 4)));
  }

  hotelHash(hotel: Hotel): number {
    return Array.from(hotel.hotelId || hotel.name).reduce((sum, char) => sum + char.charCodeAt(0), 0);
  }

  hasHighDemand(hotel: Hotel): boolean {
    return this.hotelHash(hotel) % 5 < 2;
  }

  hasSocialProof(hotel: Hotel): boolean {
    return this.hotelHash(hotel) % 10 < 4;
  }

  bookedTodayCount(hotel: Hotel): number {
    return this.hotelHash(hotel) % 8 + 2;
  }

  hasPriceDrop(hotel: Hotel): boolean {
    return this.hotelHash(hotel) % 5 === 0;
  }

  lowAvailability(hotel: Hotel): number | null {
    const available = (hotel.roomTypes ?? [])
      .filter(room => room.availableRooms > 0 && room.status !== 'Inactive')
      .map(room => room.availableRooms);
    if (!available.length) return null;
    const min = Math.min(...available);
    return min <= 3 ? min : null;
  }

  amenityPills(hotel: Hotel): string[] {
    const text = `${hotel.description} ${hotel.name}`.toLowerCase();
    const pills = ['📶 WiFi'];
    if (text.includes('pool')) pills.push('🏊 Pool');
    if (text.includes('parking')) pills.push('🅿️ Parking');
    if (text.includes('restaurant') || text.includes('dining')) pills.push('🍽️ Restaurant');
    if (pills.length < 3) pills.push('🅿️ Parking', '🍽️ Restaurant');
    return [...new Set(pills)].slice(0, 3);
  }

  get minCheckOutDate(): string {
    if (!this.search.checkIn) return this.today;
    const d = new Date(this.search.checkIn);
    d.setDate(d.getDate() + 1);
    return d.toISOString().split('T')[0];
  }

  onCheckInChange(): void {
    if (this.search.checkIn && this.search.checkOut && this.search.checkOut <= this.search.checkIn) {
      this.search.checkOut = this.minCheckOutDate;
    }
  }
}
