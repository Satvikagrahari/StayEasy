using System;
using System.Collections.Generic;
using System.Text;

namespace CatalogService.Application.DTOs.Response
{
    public class RoomTypeDto
    {
        //public Guid HotelId { get; set; }
        public Guid RoomTypeId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        //public string City { get; set; }
        public int MaxGuests { get; set; }
        public decimal PricePerNight { get; set; }
        public int TotalRooms { get; set; }
        public int AvailableRooms { get; set; }
        public string Status { get; set; }

        //public List<RoomTypeDto> RoomTypes { get; set; }
    }
}
