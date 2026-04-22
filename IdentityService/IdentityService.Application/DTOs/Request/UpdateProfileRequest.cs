using System.ComponentModel.DataAnnotations;

namespace IdentityService.Application.DTOs.Request
{
    public class UpdateProfileRequest
    {
        [EmailAddress]
        public string? Email { get; set; }

        public string? PhoneNumber { get; set; }
    }
}