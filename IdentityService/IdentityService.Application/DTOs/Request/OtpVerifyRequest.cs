using System;
using System.Collections.Generic;
using System.Text;

namespace IdentityService.Application.DTOs.Request
{
    public class OtpVerifyRequest
    {
        public string Email { get; set; }
        public string Code { get; set; }
    }
}
