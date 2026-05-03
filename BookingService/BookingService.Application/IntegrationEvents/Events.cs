using System;

namespace BookingService.Application.IntegrationEvents
{
    public class BookingItemDto
    {
        public Guid RoomTypeId { get; set; }
        public int Nights { get; set; }
    }

    public class BookingCreatedIntegrationEvent
    {
        public Guid BookingId { get; set; }
        public Guid UserId { get; set; }
        public decimal TotalAmount { get; set; }

        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<BookingItemDto> BookingItems { get; set; } = new();
    }

    public class BookingCancelledIntegrationEvent
    {
        public Guid BookingId { get; set; }
        public List<BookingItemDto> BookingItems { get; set; } = new();
    }

    public class BookingCompletedIntegrationEvent
    {
        public Guid BookingId { get; set; }
        public List<BookingItemDto> BookingItems { get; set; } = new();
    }

    public class PaymentProcessedIntegrationEvent
    {
        public Guid BookingId { get; set; }
        public bool IsSuccess { get; set; }
        public DateTime ProcessedAt { get; set; }
    }

    public class BookingPaymentFailedIntegrationEvent
    {
        public Guid BookingId { get; set; }
        public List<BookingItemDto> BookingItems { get; set; } = new();
    }
    
    public class RefundRequestedIntegrationEvent
    {
        public Guid BookingId { get; set; }
        public Guid UserId { get; set; }
        public decimal RefundAmount { get; set; }
        public DateTime RequestedAt { get; set; }
    }

    public class RefundedIntegrationEvent
    {
        public Guid BookingId { get; set; }
        public decimal RefundedAmount { get; set; }
        public DateTime RefundedAt { get; set; }
    }
}
