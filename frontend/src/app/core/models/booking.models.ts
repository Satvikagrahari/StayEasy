export interface CartItem {
  cartItemId: string;
  cartId: string;
  hotelId: string;
  roomTypeId: string;
  checkInDate: string;   // ISO date string
  checkOutDate: string;
  guests: number;
  priceSnapshot: number;
}

export interface EnrichedCartItem extends CartItem {
  hotelName?: string;
  roomName?: string;
  nights: number;
  subtotal: number;
}

export interface Cart {
  cartId: string;
  userId: string;
  status: string;
  createdAt: string;
  items: CartItem[];
}

export interface EnrichedCart extends Omit<Cart, 'items'> {
  items: EnrichedCartItem[];
}

// Request body for POST /api/cart/add
export interface AddToCartRequest {
  hotelId: string;
  roomTypeId: string;
  checkInDate: string;
  checkOutDate: string;
  guests: number;
}

export interface BookingItem {
  bookingItemId: string;
  bookingId: string;
  hotelId: string;
  roomTypeId: string;
  checkInDate: string;
  checkOutDate: string;
  nights: number;
  pricePerNight: number;
  subtotal: number;
}

export interface Booking {
  id: string;
  userId: string;
  totalAmount: number;
  bookingDate: string;
  status: string;        // "Pending" | "Confirmed" | "Cancelled" | "RefundRequested"
  cancellationDate: string | null;
  bookingItems: BookingItem[];
}

// Response from POST /api/booking/checkout
export interface CheckoutResponse {
  bookingId: string;
}
