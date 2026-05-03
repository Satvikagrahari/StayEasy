using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CatalogService.Application.DTOs.Request
{
    public class UpdateHotelRequest
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string City { get; set; }

        [Required]
        public string Country { get; set; }

        [Required]
        public string Address { get; set; }

        public string Description { get; set; }

        public int StarRating { get; set; }
        public List<string>? ImageUrls { get; set; }
    }
}