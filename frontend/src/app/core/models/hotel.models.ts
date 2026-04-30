export interface RoomType {
  roomTypeId: string;
  name: string;
  description?: string;
  maxGuests: number;
  pricePerNight: number;
  totalRooms: number;
  availableRooms: number;
  status?: string;
}

export interface Hotel {
  hotelId: string;
  name: string;
  city: string;
  country: string;
  address: string;
  description: string;
  starRating: number;
  roomTypes: RoomType[];
}

export interface CreateHotelRequest {
  name: string;
  city: string;
  country: string;
  address: string;
  description: string;
  starRating: number;
}

export interface CreateRoomTypeRequest {
  hotelId: string;
  type: string;
  description: string;
  maxGuests: number;
  pricePerNight: number;
  totalRooms: number;
  availableRooms?: number;
}

export interface UpdateRoomTypeRequest {
  type: string;
  description: string;
  maxGuests: number;
  pricePerNight: number;
  totalRooms: number;
  availableRooms?: number;
  status?: string;
}
