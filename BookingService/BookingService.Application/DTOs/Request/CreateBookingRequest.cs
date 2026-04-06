using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace BookingService.Application.DTOs.Request
{
    public class CreateBookingRequest
    {
        [Required]
        public Guid HotelId { get; set; }

        [Required]
        public DateTime CheckIn { get; set; }

        [Required]
        public DateTime CheckOut { get; set; }

        public int Guests { get; set; }
    }
}

