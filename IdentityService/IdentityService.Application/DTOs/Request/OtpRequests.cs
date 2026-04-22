    namespace IdentityService.Application.DTOs.Request
{
    public class SendOtpRequest
    {
        public string PhoneNumber { get; set; }
        public string Channel { get; set; } = "sms"; // "sms" or "whatsapp"
    }

    public class VerifyOtpRequest
    {
        public string PhoneNumber { get; set; }
        public string Code { get; set; }
    }
}
