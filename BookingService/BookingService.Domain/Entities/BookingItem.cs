using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Domain.Entities
{
    public class BookingItem
    {
        public Guid BookingItemId { get; set; }

        public Guid BookingId { get; set; }

        public Guid HotelId { get; set; }

        public Guid RoomTypeId { get; set; }

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public int Nights { get; set; }

        public decimal PricePerNight { get; set; }

        public decimal Subtotal { get; set; }

        public Booking Booking { get; set; }
    }
}
