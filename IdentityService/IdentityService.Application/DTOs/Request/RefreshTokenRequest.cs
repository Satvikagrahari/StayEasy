using System.ComponentModel.DataAnnotations;

namespace IdentityService.Application.DTOs.Request
{
    public class RefreshTokenRequest
    {
        [Required]
        public string RefreshToken { get; set; }
    }
}