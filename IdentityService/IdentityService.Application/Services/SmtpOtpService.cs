using IdentityService.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using System.Net;
using System.Net.Mail;

namespace IdentityService.Application.Services
{
    public class SmtpOtpService : IOtpService
    {
        private readonly IConfiguration _config;

        public SmtpOtpService(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendOtpAsync(string email, string code)
        {
            var host = _config["Smtp:Host"] ?? throw new ApplicationException("Smtp:Host is missing");
            var port = int.Parse(_config["Smtp:Port"] ?? "587");
            var username = _config["Smtp:Username"] ?? throw new ApplicationException("Smtp:Username is missing");
            var password = _config["Smtp:Password"] ?? throw new ApplicationException("Smtp:Password is missing");
            var fromEmail = _config["Smtp:FromEmail"] ?? username;
            var fromName = _config["Smtp:FromName"] ?? "StayEasy";
            var enableSsl = bool.Parse(_config["Smtp:EnableSsl"] ?? "true");

            using var message = new MailMessage
            {
                From = new MailAddress(fromEmail, fromName),
                Subject = "Your StayEasy verification code",
                Body = $"""
                    Hi,

                    Your StayEasy verification code is {code}.

                    This code expires in 10 minutes. If you did not create a StayEasy account, you can ignore this email.

                    StayEasy
                    """,
                IsBodyHtml = false
            };

            message.To.Add(email);

            using var client = new SmtpClient(host, port)
            {
                EnableSsl = enableSsl,
                Credentials = new NetworkCredential(username, password)
            };

            await client.SendMailAsync(message);
        }
    }
}

