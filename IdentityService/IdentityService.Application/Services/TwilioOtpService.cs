using IdentityService.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Twilio.Rest.Verify.V2.Service;
using System.Threading.Tasks;
using System;

namespace IdentityService.Application.Services
{
    public class TwilioOtpService : IOtpService
    {
        private readonly string _verifyServiceSid;

        public TwilioOtpService(IConfiguration config)
        {
            _verifyServiceSid = config["Twilio:VerifyServiceSid"] 
                ?? throw new ApplicationException("Twilio:VerifyServiceSid is missing in configuration");
        }

        public async Task SendOtpAsync(string phoneNumber, string channel)
        {
            await VerificationResource.CreateAsync(
                to: phoneNumber,
                channel: channel,
                pathServiceSid: _verifyServiceSid
            );
        }

        public async Task<bool> VerifyOtpAsync(string phoneNumber, string code)
        {
            var verificationCheck = await VerificationCheckResource.CreateAsync(
                to: phoneNumber,
                code: code,
                pathServiceSid: _verifyServiceSid
            );

            return verificationCheck.Status == "approved";
        }
    }
}
