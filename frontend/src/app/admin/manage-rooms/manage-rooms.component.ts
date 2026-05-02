import { Component, OnInit, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute } from '@angular/router';
import { Observable } from 'rxjs';
import { HotelApiService } from '../../core/services/hotel-api.service';
import { ToastService } from '../../core/services/toast.service';
import { Hotel, RoomType, UpdateRoomTypeRequest } from '../../core/models/hotel.models';

@Component({
  selector: 'app-manage-rooms',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './manage-rooms.component.html',
  styleUrl: './manage-rooms.component.css'
})
export class ManageRoomsComponent implements OnInit {
  private hotelApi = inject(HotelApiService);
  private toast = inject(ToastService);
  private fb = inject(FormBuilder);
  private route = inject(ActivatedRoute);

  hotels = signal<Hotel[]>([]);
  selectedHotel = signal<Hotel | null>(null);
  isLoading = signal(true);
  showForm = signal(false);
  editingId = signal<string | null>(null);
  saving = signal(false);
  deletingRoomId = signal<string | null>(null);
  deleteTarget = signal<RoomType | null>(null);

  roomForm = this.fb.group({
    type: ['', [Validators.required, Validators.minLength(2)]],
    description: [''],
    maxGuests: [2, [Validators.required, Validators.min(1)]],
    pricePerNight: [0, [Validators.required, Validators.min(1)]],
    totalRooms: [1, [Validators.required, Validators.min(1)]],
    availableRooms: [1, [Validators.required, Validators.min(0)]],
    status: ['Active']
  });

  ngOnInit(): void {
    this.loadHotels();
  }

  loadHotels(): void {
    this.isLoading.set(true);
    this.hotelApi.getHotels().subscribe({
      next: hotels => {
        this.hotels.set(hotels);
        this.isLoading.set(false);
        
        // Priority 1: Check query params for initial selection
        const queryId = this.route.snapshot.queryParams['hotelId'];
        if (queryId) {
          this.selectHotelId(queryId);
          return;
        }

        // Priority 2: Check existing selection (refresh case)
        const selected = this.selectedHotel();
        if (selected) {
          this.selectedHotel.set(hotels.find(h => h.hotelId === selected.hotelId) ?? null);
        }
      },
      error: () => this.isLoading.set(false)
    });
  }

  selectHotelId(hotelId: string): void {
    this.selectedHotel.set(this.hotels().find(h => h.hotelId === hotelId) ?? null);
    this.showForm.set(false);
  }

  openCreate(): void {
    const hotel = this.selectedHotel();
    if (!hotel) return;
    this.editingId.set(null);
    this.roomForm.reset({
      type: '',
      description: '',
      maxGuests: 2,
      pricePerNight: 0,
      totalRooms: 1,
      availableRooms: 1,
      status: 'Active'
    });
    this.showForm.set(true);
  }

  openEdit(room: RoomType): void {
    this.editingId.set(room.roomTypeId);
    this.roomForm.patchValue({
      type: room.name,
      description: room.description ?? '',
      maxGuests: room.maxGuests || 1,
      pricePerNight: room.pricePerNight,
      totalRooms: room.totalRooms,
      availableRooms: room.availableRooms,
      status: room.status ?? 'Active'
    });
    this.showForm.set(true);
  }

  save(): void {
    const hotel = this.selectedHotel();
    if (!hotel || this.roomForm.invalid) {
      this.roomForm.markAllAsTouched();
      return;
    }

    this.saving.set(true);
    const value = this.roomForm.getRawValue();
    const request: UpdateRoomTypeRequest = {
      type: value.type!,
      description: value.description ?? '',
      maxGuests: Number(value.maxGuests),
      pricePerNight: Number(value.pricePerNight),
      totalRooms: Number(value.totalRooms),
      availableRooms: Number(value.availableRooms),
      status: value.status ?? 'Active'
    };

    const id = this.editingId();
    const op: Observable<unknown> = id
      ? this.hotelApi.updateRoomType(id, request)
      : this.hotelApi.createRoomType({ hotelId: hotel.hotelId, ...request });

    op.subscribe({
      next: () => {
        this.saving.set(false);
        this.showForm.set(false);
        this.toast.success(id ? 'Room type updated.' : 'Room type created.');
        this.refreshHotel(hotel.hotelId);
      },
      error: () => {
        this.saving.set(false);
      }
    });
  }

  deleteRoom(room: RoomType): void {
    this.deletingRoomId.set(room.roomTypeId);
    this.hotelApi.deleteRoomType(room.roomTypeId).subscribe({
      next: () => {
        this.deletingRoomId.set(null);
        this.deleteTarget.set(null);
        this.toast.success('Room type deleted.');
        const hotel = this.selectedHotel();
        if (hotel) {
          this.refreshHotel(hotel.hotelId);
        }
      },
      error: () => {
        this.deletingRoomId.set(null);
      }
    });
  }

  confirmDeleteRoom(): void {
    const room = this.deleteTarget();
    if (!room) return;
    this.deleteRoom(room);
  }

  private refreshHotel(hotelId: string): void {
    this.hotelApi.getHotelById(hotelId).subscribe({
      next: hotel => {
        this.selectedHotel.set(hotel);
        this.hotels.update(list => list.map(item => item.hotelId === hotel.hotelId ? hotel : item));
      }
    });
  }
}
