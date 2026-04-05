using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Domain.Entities
{
    public class User
    {
        public Guid Id { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; }
        public string PhoneNumber { get; set; }
        public string Role { get; set; } = "Guest";
        public bool IsVerified { get; set; }
    }
}
