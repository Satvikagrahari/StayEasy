namespace BookingService.Domain.Entities
{
    public enum BookingStatus
    {
        Pending,
        Confirmed,
        Cancelled,
        RefundRequested,
        Refunded,
        Failed,
        Completed
    }
}
