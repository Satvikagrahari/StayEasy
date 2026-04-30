namespace IdentityService.Application.DTOs.Request
{
    public class SendOtpRequest
    {
        public string Email { get; set; }
    }

    public class VerifyOtpRequest
    {
        public string Email { get; set; }
        public string Code { get; set; }
    }
}
