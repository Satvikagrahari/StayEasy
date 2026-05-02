import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormArray, FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { HotelApiService } from '../../core/services/hotel-api.service';
import { ToastService } from '../../core/services/toast.service';
import { CreateHotelRequest, Hotel } from '../../core/models/hotel.models';

@Component({
  selector: 'app-hotels',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, FormsModule],
  templateUrl: './hotels.component.html',
  styleUrl: './hotels.component.css'
})
export class HotelsComponent implements OnInit {
  private hotelApi = inject(HotelApiService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);
  private router = inject(Router);

  hotels = signal<Hotel[]>([]);
  isLoading = signal(true);
  error = signal<string | null>(null);
  showForm = signal(false);
  editingId = signal<string | null>(null);
  saving = signal(false);
  deletingId = signal<string | null>(null);
  deactivateTarget = signal<Hotel | null>(null);
  search = '';
  statusFilter: 'all' | 'active' | 'inactive' = 'all';

  hotelForm = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2)]],
    city: ['', [Validators.required, Validators.minLength(2)]],
    country: ['', [Validators.required, Validators.minLength(2)]],
    address: ['', Validators.required],
    description: [''],
    starRating: [3, [Validators.required, Validators.min(1), Validators.max(5)]],
    imageUrls: this.fb.array([this.fb.control('')])
  });

  roomDraft = {
    name: '',
    description: '',
    capacity: 2,
    price: 6000,
    availability: 5
  };

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.hotelApi.getHotels().subscribe({
      next: hotels => {
        this.hotels.set(hotels);
        this.isLoading.set(false);
      },
      error: () => {
        this.error.set('Failed to load hotels.');
        this.isLoading.set(false);
      }
    });
  }

  openCreate(): void {
    this.editingId.set(null);
    this.hotelForm.reset({ description: '', starRating: 3 });
    this.setImageFields(['']);
    this.roomDraft = { name: '', description: '', capacity: 2, price: 6000, availability: 5 };
    this.showForm.set(true);
  }

  openEdit(hotel: Hotel): void {
    this.editingId.set(hotel.hotelId);
    this.hotelForm.patchValue({
      name: hotel.name,
      city: hotel.city,
      country: hotel.country,
      address: hotel.address,
      description: hotel.description,
      starRating: hotel.starRating || 3
    });
    this.setImageFields([]);
    this.showForm.set(true);
  }

  save(): void {
    if (this.hotelForm.invalid) {
      this.hotelForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const value = this.hotelForm.getRawValue();
    const id = this.editingId();
    const request: CreateHotelRequest = {
      name: value.name!,
      city: value.city!,
      country: value.country!,
      address: value.address!,
      description: value.description ?? '',
      starRating: Number(value.starRating),
      imageUrls: id ? undefined : this.imageUrls.controls
        .map(control => (control.value ?? '').trim())
        .filter(url => !!url)
    };

    const op = id ? this.hotelApi.updateHotel(id, request) : this.hotelApi.createHotel(request);

    op.subscribe({
      next: () => {
        this.saving.set(false);
        this.showForm.set(false);
        this.toast.success(id ? 'Hotel updated.' : 'Hotel created.');
        this.load();
      },
      error: () => {
        this.saving.set(false);
      }
    });
  }

  delete(id: string): void {
    this.deletingId.set(id);
    this.hotelApi.deleteHotel(id).subscribe({
      next: () => {
        this.deletingId.set(null);
        this.toast.success('Hotel deleted.');
        this.load();
      },
      error: () => {
        this.deletingId.set(null);
      }
    });
  }

  confirmDeactivate(): void {
    const target = this.deactivateTarget();
    if (!target) return;
    this.deactivateTarget.set(null);
    this.delete(target.hotelId);
  }

  filteredHotels(): Hotel[] {
    const q = this.search.trim().toLowerCase();
    return this.hotels().filter(hotel => {
      const active = this.isActive(hotel);
      const matchesStatus = this.statusFilter === 'all' || (this.statusFilter === 'active' ? active : !active);
      const matchesText = !q || [hotel.name, hotel.city, hotel.country, hotel.address].some(v => (v || '').toLowerCase().includes(q));
      return matchesStatus && matchesText;
    });
  }

  isActive(hotel: Hotel): boolean {
    return hotel.roomTypes?.some(room => room.status !== 'Inactive') ?? true;
  }

  setRating(value: number): void {
    this.hotelForm.patchValue({ starRating: value });
  }

  ratingValue(): number {
    return Number(this.hotelForm.value.starRating ?? 0);
  }

  get imageUrls(): FormArray {
    return this.hotelForm.get('imageUrls') as FormArray;
  }

  addImageField(): void {
    this.imageUrls.push(this.fb.control(''));
  }

  removeImageField(index: number): void {
    this.imageUrls.removeAt(index);
  }

  private setImageFields(urls: string[]): void {
    this.imageUrls.clear();
    urls.forEach(url => this.imageUrls.push(this.fb.control(url)));
  }

  manageRooms(hotel: Hotel): void {
    this.router.navigate(['/admin/manage-rooms'], { queryParams: { hotelId: hotel.hotelId } });
  }
}
