using System.ComponentModel.DataAnnotations;

namespace IdentityService.Application.DTOs.Request
{
    public class UpdateProfileRequest
    {
        public string? UserName { get; set; }

        [EmailAddress]
        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }
    }
}
