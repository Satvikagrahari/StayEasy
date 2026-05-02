using System;
using System.Collections.Generic;
using System.Text;

namespace CatalogService.Application.DTOs.Response
{
    public class HotelResponseDto
    {
        public Guid HotelId { get; set; }
        public string Name { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Address { get; set; }
        public string Description { get; set; }
        public int StarRating { get; set; }
        public List<string> ImageUrls { get; set; } = new();

        public List<RoomTypeDto> RoomTypes { get; set; }
    }
}
