using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BookingService.Application.DTOs.Request
{

    public class CreateCartItemRequest
    {
        [Required]
        public Guid HotelId { get; set; }

        [Required]
        public Guid RoomTypeId { get; set; }

        [Required]
        public DateTime CheckInDate { get; set; }

        [Required]
        public DateTime CheckOutDate { get; set; }

        public int Guests { get; set; }
    }
}
