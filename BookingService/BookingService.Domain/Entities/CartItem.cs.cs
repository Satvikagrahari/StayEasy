using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Domain.Entities
{
    public class CartItem
    {
        public Guid CartItemId { get; set; }

        public Guid CartId { get; set; }

        public Guid HotelId { get; set; }

        public Guid RoomTypeId { get; set; }

        public DateTime CheckInDate { get; set; }

        public DateTime CheckOutDate { get; set; }

        public int Guests { get; set; }

        public decimal PriceSnapshot { get; set; }
    }
}
