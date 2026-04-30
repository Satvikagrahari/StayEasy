import { Component, inject, OnInit, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { HotelApiService } from '../../core/services/hotel-api.service';
import { RoomType } from '../../core/models/hotel.models';

@Component({
  selector: 'app-room-details',
  standalone: true,
  imports: [CommonModule, RouterLink],
  templateUrl: './room-details.component.html',
  styleUrl: './room-details.component.css'
})
export class RoomDetailsComponent implements OnInit {
  private route = inject(ActivatedRoute);
  private hotelApi = inject(HotelApiService);

  room = signal<RoomType | null>(null);
  isLoading = signal(true);
  error = signal<string | null>(null);

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id')!;
    this.hotelApi.getRoomTypeById(id).subscribe({
      next: (r) => { this.room.set(r); this.isLoading.set(false); },
      error: () => { this.error.set('Room not found.'); this.isLoading.set(false); }
    });
  }
}
