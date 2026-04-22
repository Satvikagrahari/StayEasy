using System;
using System.Collections.Generic;
using System.Text;

namespace CatalogService.Domain.Entities
{
    public class Hotel
    {
        public Guid HotelId { get; set; }

        public string Name { get; set; }

        public string City { get; set; }

        public string Country { get; set; }

        public string Address { get; set; }

        public string Description { get; set; }

        public int StarRating { get; set; }

        public bool IsActive { get; set; } = true;

        public List<RoomType> RoomTypes { get; set; }
    }
}
