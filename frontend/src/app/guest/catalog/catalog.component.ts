import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { HotelApiService } from '../../core/services/hotel-api.service';
import { Hotel } from '../../core/models/hotel.models';

@Component({
  selector: 'app-catalog',
  standalone: true,
  imports: [CommonModule, RouterLink, FormsModule],
  templateUrl: './catalog.component.html',
  styleUrl: './catalog.component.css'
})
export class CatalogComponent implements OnInit {
  private hotelApi = inject(HotelApiService);
  private route = inject(ActivatedRoute);

  hotels = signal<Hotel[]>([]);
  filtered = signal<Hotel[]>([]);
  isLoading = signal(true);
  error = signal<string | null>(null);
  viewMode = signal<'grid' | 'list'>('grid');
  searchTerm = '';
  minPriceFilter: number | null = null;
  maxPriceFilter: number | null = null;
  starFilter = 0;
  selectedRoomTypes: string[] = [];
  sortBy = 'priceAsc';
  checkInDate = '';
  checkOutDate = '';
  availableRoomTypes = signal<string[]>([]);
  private debounceId: ReturnType<typeof setTimeout> | null = null;

  ngOnInit(): void {
    this.searchTerm = this.route.snapshot.queryParamMap.get('city') ?? '';
    this.checkInDate = this.route.snapshot.queryParamMap.get('checkIn') ?? '';
    this.checkOutDate = this.route.snapshot.queryParamMap.get('checkOut') ?? '';
    this.hotelApi.getHotels().subscribe({
      next: (data) => {
        this.hotels.set(data);
        this.availableRoomTypes.set([...new Set(data.flatMap(h => h.roomTypes?.map(r => r.name) ?? []))].sort());
        this.applyFilters();
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Failed to load hotels.');
        this.isLoading.set(false);
      }
    });
  }

  hotelQueryParams(): Record<string, string> | null {
    const queryParams: Record<string, string> = {};
    if (this.checkInDate) queryParams['checkIn'] = this.checkInDate;
    if (this.checkOutDate) queryParams['checkOut'] = this.checkOutDate;
    return Object.keys(queryParams).length ? queryParams : null;
  }

  dateSummary(): string {
    if (!this.checkInDate && !this.checkOutDate) return '';
    if (this.checkInDate && this.checkOutDate) {
      return `${this.formatShortDate(this.checkInDate)} - ${this.formatShortDate(this.checkOutDate)}`;
    }
    return this.checkInDate ? `${this.formatShortDate(this.checkInDate)} - checkout needed` : `Check-out ${this.formatShortDate(this.checkOutDate)}`;
  }

  private formatShortDate(value: string): string {
    return new Date(`${value}T00:00:00`).toLocaleDateString('en-IN', {
      day: 'numeric',
      month: 'short'
    });
  }

  debounceFilters(): void {
    if (this.debounceId) clearTimeout(this.debounceId);
    this.debounceId = setTimeout(() => this.applyFilters(), 300);
  }

  applyFilters(): void {
    let result = this.hotels();
    if (this.searchTerm.trim()) {
      const q = this.searchTerm.toLowerCase();
      result = result.filter(h =>
        h.city.toLowerCase().includes(q) ||
        h.name.toLowerCase().includes(q) ||
        h.description.toLowerCase().includes(q)
      );
    }
    if (this.minPriceFilter !== null) {
      result = result.filter(h => this.minPrice(h) >= (this.minPriceFilter ?? 0));
    }
    if (this.maxPriceFilter !== null) {
      result = result.filter(h => this.minPrice(h) <= (this.maxPriceFilter ?? Number.MAX_SAFE_INTEGER));
    }
    if (this.starFilter > 0) {
      result = result.filter(h => (h.starRating || 0) >= this.starFilter);
    }
    if (this.selectedRoomTypes.length) {
      result = result.filter(h => h.roomTypes.some(r => this.selectedRoomTypes.includes(r.name)));
    }
    result = [...result].sort((a, b) => {
      if (this.sortBy === 'priceDesc') return this.minPrice(b) - this.minPrice(a);
      if (this.sortBy === 'ratingDesc') return (b.starRating || 0) - (a.starRating || 0);
      if (this.sortBy === 'nameAsc') return a.name.localeCompare(b.name);
      return this.minPrice(a) - this.minPrice(b);
    });
    this.filtered.set(result);
  }

  clearFilters(): void {
    this.searchTerm = '';
    this.minPriceFilter = null;
    this.maxPriceFilter = null;
    this.starFilter = 0;
    this.selectedRoomTypes = [];
    this.sortBy = 'priceAsc';
    this.applyFilters();
  }

  setStarFilter(rating: number): void {
    this.starFilter = this.starFilter === rating ? 0 : rating;
    this.applyFilters();
  }

  toggleRoomType(roomType: string, checked: boolean): void {
    this.selectedRoomTypes = checked
      ? [...this.selectedRoomTypes, roomType]
      : this.selectedRoomTypes.filter(type => type !== roomType);
    this.applyFilters();
  }

  isRoomSelected(roomType: string): boolean {
    return this.selectedRoomTypes.includes(roomType);
  }

  minPrice(hotel: Hotel): number {
    const rooms = hotel.roomTypes.filter(room => room.availableRooms > 0 && room.status !== 'Inactive');
    if (!rooms.length) return 0;
    return Math.min(...rooms.map(r => r.pricePerNight));
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
}
