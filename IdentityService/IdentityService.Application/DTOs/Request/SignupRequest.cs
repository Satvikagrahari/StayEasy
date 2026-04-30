using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Text;

namespace IdentityService.Application.DTOs.Request
{
    public class SignupRequest
    {
        [Required]
        [MinLength(3)]
        [MaxLength(80)]
        public string UserName { get; set; }

        [Required]
        [EmailAddress]
        public string Email { get; set; }

        [Required]
        [MinLength(8)]
        [RegularExpression(@"^(?=.*[A-Z])(?=.*\d).+$",
            ErrorMessage = "Password must contain at least 1 uppercase and 1 number")]
        public string Password { get; set; }

        [Required]
        [Phone]
        public string PhoneNumber { get; set; }
        //public string? Role { get; set; }
    }
}
