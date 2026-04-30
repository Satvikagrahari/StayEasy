namespace IdentityService.Application.DTOs.Request
{
    public class PasswordResetSendOtpRequest
    {
        public string Email { get; set; }
    }

    public class PasswordResetVerifyOtpRequest
    {
        public string Email { get; set; }
        public string Code { get; set; }
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; }
        public string Code { get; set; }
        public string NewPassword { get; set; }
    }
}
