import { Component, HostListener, Input, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { NavigationEnd, Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AsyncPipe } from '@angular/common';
import { filter } from 'rxjs';
import { AuthService } from '../../core/services/auth.service';
import { CartService } from '../../core/services/cart.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [CommonModule, FormsModule, RouterLink, RouterLinkActive, AsyncPipe],
  templateUrl: './navbar.component.html',
  styleUrl: './navbar.component.css'
})
export class NavbarComponent {
  @Input() adminMode = false;
  @Input() adminMobileOpen = false;

  private authService = inject(AuthService);
  private cartService = inject(CartService);
  private router = inject(Router);

  currentUser$ = this.authService.currentUser$;
  isAdmin$ = this.authService.isAdmin$;
  cartCount$ = this.cartService.count$;

  searchCity = '';
  checkInDate = '';
  checkOutDate = '';
  datePickerOpen = false;
  dropdownOpen = false;
  mobileMenuOpen = false;
  isScrolled = false;
  cartPulse = false;
  readonly today = new Date().toISOString().split('T')[0];

  constructor() {
    this.cartService.pulse$.subscribe(value => {
      if (!value) return;
      this.cartPulse = false;
      requestAnimationFrame(() => {
        this.cartPulse = true;
        window.setTimeout(() => this.cartPulse = false, 550);
      });
    });

    this.router.events.pipe(filter(event => event instanceof NavigationEnd)).subscribe(() => {
      this.dropdownOpen = false;
      this.mobileMenuOpen = false;
    });
  }

  @HostListener('window:scroll')
  onScroll(): void {
    this.isScrolled = window.scrollY > 8;
  }

  @HostListener('document:click', ['$event'])
  onDocumentClick(event: MouseEvent): void {
    const target = event.target as HTMLElement | null;
    if (!target?.closest('.avatar-menu')) {
      this.dropdownOpen = false;
    }
    if (!target?.closest('.nav-date-field')) {
      this.datePickerOpen = false;
    }
  }

  goSearch(): void {
    const queryParams: Record<string, string> = { focus: 'search' };
    if (this.searchCity.trim()) {
      queryParams['city'] = this.searchCity.trim();
    }
    if (this.checkInDate) {
      queryParams['checkIn'] = this.checkInDate;
    }
    if (this.checkOutDate) {
      queryParams['checkOut'] = this.checkOutDate;
    }
    this.router.navigate(['/hotels'], { queryParams });
  }

  toggleDatePicker(event: MouseEvent): void {
    event.stopPropagation();
    this.datePickerOpen = !this.datePickerOpen;
  }

  get dateLabel(): string {
    if (this.checkInDate && this.checkOutDate) {
      return `${this.shortDate(this.checkInDate)} - ${this.shortDate(this.checkOutDate)}`;
    }
    if (this.checkInDate) {
      return `${this.shortDate(this.checkInDate)} - Add checkout`;
    }
    return 'Add dates';
  }

  get minCheckOutDate(): string {
    if (!this.checkInDate) return this.today;
    const d = new Date(this.checkInDate);
    d.setDate(d.getDate() + 1);
    return d.toISOString().split('T')[0];
  }

  onCheckInChange(): void {
    if (this.checkInDate && this.checkOutDate && this.checkOutDate <= this.checkInDate) {
      this.checkOutDate = this.minCheckOutDate;
    }
  }

  private shortDate(value: string): string {
    return new Date(`${value}T00:00:00`).toLocaleDateString('en-IN', {
      day: 'numeric',
      month: 'short'
    });
  }

  toggleDropdown(event: MouseEvent): void {
    event.stopPropagation();
    this.dropdownOpen = !this.dropdownOpen;
  }

  initials(username?: string | null, email?: string | null): string {
    const label = (username || email || 'SE').trim();
    return label.slice(0, 2).toUpperCase();
  }

  logout(): void {
    this.dropdownOpen = false;
    this.mobileMenuOpen = false;
    this.authService.logout();
  }
}
