using System;
using System.Collections.Generic;

namespace AdminService.Application.DTOs
{
    public class BookingDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public DateTime BookingDate { get; set; }
        public string Status { get; set; } = string.Empty;
        public List<BookingItemDto> BookingItems { get; set; } = new();
    }

    public class BookingItemDto
    {
        public Guid BookingItemId { get; set; }
        public Guid HotelId { get; set; }
        public Guid RoomTypeId { get; set; }
        public DateTime CheckInDate { get; set; }
        public DateTime CheckOutDate { get; set; }
        public int Nights { get; set; }
        public decimal PricePerNight { get; set; }
        public decimal Subtotal { get; set; }
    }
}
