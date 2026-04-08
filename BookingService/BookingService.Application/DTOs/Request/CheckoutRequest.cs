using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Application.DTOs.Request
{
    public class CheckoutRequest
    {
        public string PaymentMethod { get; set; } // optional for now
    }
}
