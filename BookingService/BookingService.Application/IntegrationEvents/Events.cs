using System;

namespace BookingService.Application.IntegrationEvents
{
    public class BookingCreatedIntegrationEvent
    {
        public Guid BookingId { get; set; }
        public Guid UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class PaymentProcessedIntegrationEvent
    {
        public Guid BookingId { get; set; }
        public bool IsSuccess { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}
