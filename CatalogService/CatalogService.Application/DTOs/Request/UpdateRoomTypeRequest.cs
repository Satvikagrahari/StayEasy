using System.ComponentModel.DataAnnotations;

namespace CatalogService.Application.DTOs.Request
{
    public class UpdateRoomTypeRequest
    {
        [Required]
        public string Type { get; set; }

        public string Description { get; set; }

        [Required]
        public int MaxGuests { get; set; }

        [Required]
        public decimal PricePerNight { get; set; }

        [Required]
        public int TotalRooms { get; set; }

        public int? AvailableRooms { get; set; }

        public string? Status { get; set; }
    }
}