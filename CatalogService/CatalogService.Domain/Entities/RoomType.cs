using System;
using System.Collections.Generic;
using System.Text;

namespace CatalogService.Domain.Entities
{
    public class RoomType
    {
        public Guid RoomTypeId { get; set; }
        public Guid HotelId { get; set; }

        public string Type { get; set; } // Deluxe, Suite
        public string Description { get; set; }

        public int MaxGuests { get; set; }

        public decimal PricePerNight { get; set; }

        public int TotalRooms { get; set; }

        public int AvailableRooms { get; set; }

        public string Status { get; set; } = "Active";

        public Hotel Hotel { get; set; } // navigation
    }
}
