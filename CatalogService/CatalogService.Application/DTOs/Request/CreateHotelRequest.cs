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

        public string Address { get; set; }

        public decimal PricePerNight { get; set; }
    }
}
