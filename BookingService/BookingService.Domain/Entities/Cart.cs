using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Domain.Entities
{
    public class Cart
    {
        public Guid CartId { get; set; }

        public Guid UserId { get; set; }

        public string Status { get; set; } = "Active";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<CartItem> Items { get; set; }
    }
}
