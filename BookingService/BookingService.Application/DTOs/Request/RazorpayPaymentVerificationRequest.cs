namespace BookingService.Application.DTOs.Request
{
    public class RazorpayPaymentVerificationRequest
    {
        public string RazorpayPaymentId { get; set; } = string.Empty;
        public string RazorpayOrderId { get; set; } = string.Empty;
        public string RazorpaySignature { get; set; } = string.Empty;
        public Guid BookingId { get; set; }
    }
}
