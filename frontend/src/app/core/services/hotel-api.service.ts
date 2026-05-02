import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, map } from 'rxjs';
import { CreateHotelRequest, CreateRoomTypeRequest, Hotel, RoomType, UpdateRoomTypeRequest } from '../models/hotel.models';
import { environment } from '../../../environments/environment';

const normalizeHotel = (hotel: Hotel): Hotel => ({
  ...hotel,
  country: hotel.country ?? '',
  address: hotel.address ?? '',
  description: hotel.description ?? '',
  starRating: hotel.starRating ?? 0,
  imageUrls: hotel.imageUrls ?? [],
  roomTypes: hotel.roomTypes ?? []
});

@Injectable({
  providedIn: 'root'
})
export class HotelApiService {
  private http = inject(HttpClient);
  private readonly BASE = `${environment.gatewayUrl}/gateway/catalog`;

  getHotels(): Observable<Hotel[]> {
    return this.http.get<Hotel[]>(this.BASE).pipe(map(hotels => (hotels ?? []).map(normalizeHotel)));
  }

  getHotelsByRoomType(roomType: string): Observable<Hotel[]> {
    return this.http.get<Hotel[]>(`${this.BASE}/filter`, { params: { roomType } }).pipe(
      map(hotels => (hotels ?? []).map(normalizeHotel))
    );
  }

  getHotelById(id: string): Observable<Hotel> {
    return this.http.get<Hotel>(`${this.BASE}/${id}`).pipe(map(normalizeHotel));
  }

  getRoomTypeById(roomTypeId: string): Observable<RoomType> {
    return this.http.get<RoomType>(`${this.BASE}/rooms/${roomTypeId}`);
  }

  createHotel(request: CreateHotelRequest): Observable<void> {
    return this.http.post<void>(this.BASE, request);
  }

  updateHotel(id: string, request: CreateHotelRequest): Observable<void> {
    return this.http.put<void>(`${this.BASE}/${id}`, request);
  }

  deleteHotel(id: string): Observable<void> {
    return this.http.delete<void>(`${this.BASE}/delete/hotel/${id}`);
  }

  createRoomType(request: CreateRoomTypeRequest): Observable<string> {
    return this.http.post(`${this.BASE}/rooms`, request, { responseType: 'text' });
  }

  updateRoomType(id: string, request: UpdateRoomTypeRequest): Observable<void> {
    return this.http.put<void>(`${this.BASE}/rooms/${id}`, request);
  }

  deleteRoomType(id: string): Observable<void> {
    return this.http.delete<void>(`${this.BASE}/delete/roomtype/${id}`);
  }
}
