using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json.Serialization;

namespace BookingService.Domain.Entities
{
    public class Booking
    {
        public Guid Id { get; set; }

        public Guid UserId { get; set; }

        [JsonIgnore]
        public Guid HotelId { get; set; }

        [JsonIgnore]
        public DateTime CheckIn { get; set; }

        [JsonIgnore]
        public DateTime CheckOut { get; set; }

        [JsonIgnore]
        public int Guests { get; set; }

        //public decimal TotalPrice { get; set; }
        public decimal TotalAmount { get; set; }

        public DateTime BookingDate { get; set; } = DateTime.UtcNow;
        public List<BookingItem> BookingItems { get; set; }

        public BookingStatus Status { get; set; } = BookingStatus.Pending;
        public DateTime? CancellationDate { get; set; }
    }
}
