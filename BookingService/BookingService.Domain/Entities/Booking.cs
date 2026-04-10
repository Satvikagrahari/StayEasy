using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Domain.Entities
{
    public class Booking
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        public Guid HotelId { get; set; }

        public DateTime CheckIn { get; set; }

        public DateTime CheckOut { get; set; }

        public int Guests { get; set; }

        //public decimal TotalPrice { get; set; }
        public decimal TotalAmount { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.UtcNow;
        public List<BookingItem> BookingItems { get; set; }

        public string Status { get; set; } = "Pending";
    }
}
