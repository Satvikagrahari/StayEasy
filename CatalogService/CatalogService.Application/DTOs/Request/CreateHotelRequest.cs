using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;


namespace CatalogService.Application.DTOs.Request
{

    public class CreateHotelRequest
    {
        [Required]
        public string Name { get; set; }

        [Required]
        public string City { get; set; }
        [Required]
        public string Address { get; set; }

        public string Description { get; set; }

        public int StarRating { get; set; }
        [Required]
        public string Country { get; set; }
    }
}
