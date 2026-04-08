using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace CatalogService.Application.DTOs.Request
{
    public class CreateRoomTypeRequest
    {
        [Required]
        public Guid HotelId { get; set; }

        //[Required]
        //public string RoomTypeId { get; set; }

        [Required]
        public string Name { get; set; }

        public string Description { get; set; }

        [Required]
        public int MaxGuests { get; set; }

        [Required]
        public decimal PricePerNight { get; set; }

        [Required]
        public int TotalRooms { get; set; }
    }
}
