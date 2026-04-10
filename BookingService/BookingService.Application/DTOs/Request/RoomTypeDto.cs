using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Application.DTOs.Request
{
    public class RoomTypeDto
    {
        public Guid RoomTypeId { get; set; }
        public decimal PricePerNight { get; set; }
    }
}
