using BookingService.Application.Interfaces.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Net;
using System.Net.Mail;
using System.Threading.Tasks;

namespace BookingService.Infrastructure.Services
{
    public class EmailService : IEmailService
    {
        private readonly IConfiguration _config;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IConfiguration config, ILogger<EmailService> logger)
        {
            _config = config;
            _logger = logger;
        }

        public async Task SendBookingConfirmationAsync(
            string toEmail,
            Guid bookingId,
            decimal totalAmount,
            DateTime bookingDate)
        {
            var smtpSection = _config.GetSection("Smtp");
            var host      = smtpSection["Host"]!;
            var port      = int.Parse(smtpSection["Port"]!);
            var fromEmail = smtpSection["FromEmail"]!;
            var fromName  = smtpSection["FromName"] ?? "StayEasy";
            // Strip spaces — Gmail app passwords are sometimes copied with spaces
            var password  = (smtpSection["Password"] ?? "").Replace(" ", "");
            var enableSsl = bool.Parse(smtpSection["EnableSsl"] ?? "true");

            _logger.LogInformation("📤 Sending email via {Host}:{Port} from {FromEmail}", host, port, fromEmail);

            var subject = "✅ Your StayEasy Booking is Confirmed!";
            var body    = BuildEmailBody(bookingId, totalAmount, bookingDate);

            using var client = new SmtpClient(host, port)
            {
                UseDefaultCredentials = false,
                Credentials = new NetworkCredential(fromEmail, password),
                EnableSsl   = enableSsl
            };

            var message = new MailMessage
            {
                From       = new MailAddress(fromEmail, fromName),
                Subject    = subject,
                Body       = body,
                IsBodyHtml = true
            };
            message.To.Add(toEmail);

            try
            {
                await client.SendMailAsync(message);
                _logger.LogInformation("📧 Confirmation email sent to {Email} for Booking {BookingId}.", toEmail, bookingId);
            }
            catch (Exception ex)
            {
                // Log and swallow — email failure should not roll back the booking confirmation
                _logger.LogError(ex, "Failed to send confirmation email to {Email} for Booking {BookingId}.", toEmail, bookingId);
            }
        }

        private static string BuildEmailBody(Guid bookingId, decimal totalAmount, DateTime bookingDate)
        {
            return $"""
                <!DOCTYPE html>
                <html lang="en">
                <head>
                  <meta charset="UTF-8" />
                  <meta name="viewport" content="width=device-width, initial-scale=1.0"/>
                  <title>Booking Confirmation</title>
                </head>
                <body style="margin:0;padding:0;background:#f4f6f9;font-family:'Segoe UI',Arial,sans-serif;">
                  <table width="100%" cellpadding="0" cellspacing="0" style="background:#f4f6f9;padding:40px 0;">
                    <tr>
                      <td align="center">
                        <table width="600" cellpadding="0" cellspacing="0"
                               style="background:#ffffff;border-radius:12px;overflow:hidden;
                                      box-shadow:0 4px 20px rgba(0,0,0,0.08);">

                          <!-- Header -->
                          <tr>
                            <td style="background:linear-gradient(135deg,#1a73e8,#0d47a1);
                                       padding:36px 40px;text-align:center;">
                              <h1 style="color:#ffffff;margin:0;font-size:28px;font-weight:700;
                                         letter-spacing:1px;">🏨 StayEasy</h1>
                              <p style="color:#c8d8ff;margin:8px 0 0;font-size:14px;">
                                Your home away from home
                              </p>
                            </td>
                          </tr>

                          <!-- Success Banner -->
                          <tr>
                            <td style="background:#e8f5e9;padding:20px 40px;text-align:center;
                                       border-bottom:3px solid #4caf50;">
                              <p style="margin:0;font-size:18px;font-weight:600;color:#2e7d32;">
                                ✅ Booking Confirmed!
                              </p>
                            </td>
                          </tr>

                          <!-- Body -->
                          <tr>
                            <td style="padding:36px 40px;">
                              <p style="font-size:15px;color:#444;line-height:1.7;margin:0 0 24px;">
                                Hi there! Your booking has been <strong>confirmed</strong> and
                                payment has been processed successfully. Get ready for a wonderful stay!
                              </p>

                              <!-- Details Card -->
                              <table width="100%" cellpadding="0" cellspacing="0"
                                     style="background:#f8f9ff;border-radius:8px;
                                            border:1px solid #e0e4f0;margin-bottom:24px;">
                                <tr>
                                  <td style="padding:24px;">
                                    <h3 style="margin:0 0 16px;color:#1a73e8;font-size:15px;
                                               text-transform:uppercase;letter-spacing:0.5px;">
                                      Booking Details
                                    </h3>
                                    <table width="100%" cellpadding="8" cellspacing="0">
                                      <tr>
                                        <td style="color:#666;font-size:14px;width:40%;">
                                          Booking ID
                                        </td>
                                        <td style="color:#222;font-size:14px;font-weight:600;
                                                   word-break:break-all;">
                                          {bookingId}
                                        </td>
                                      </tr>
                                      <tr style="background:#fff;">
                                        <td style="color:#666;font-size:14px;">Booking Date</td>
                                        <td style="color:#222;font-size:14px;font-weight:600;">
                                          {bookingDate:dd MMM yyyy, hh:mm tt} UTC
                                        </td>
                                      </tr>
                                      <tr>
                                        <td style="color:#666;font-size:14px;">Total Amount</td>
                                        <td style="color:#1a73e8;font-size:16px;font-weight:700;">
                                          ₹{totalAmount:N2}
                                        </td>
                                      </tr>
                                      <tr style="background:#fff;">
                                        <td style="color:#666;font-size:14px;">Status</td>
                                        <td>
                                          <span style="background:#e8f5e9;color:#2e7d32;
                                                       padding:3px 10px;border-radius:20px;
                                                       font-size:13px;font-weight:600;">
                                            Confirmed
                                          </span>
                                        </td>
                                      </tr>
                                    </table>
                                  </td>
                                </tr>
                              </table>

                              <p style="font-size:14px;color:#888;line-height:1.6;margin:0;">
                                If you have any questions about your booking, please contact our
                                support team. We're happy to help!
                              </p>
                            </td>
                          </tr>

                          <!-- Footer -->
                          <tr>
                            <td style="background:#f0f4ff;padding:20px 40px;text-align:center;
                                       border-top:1px solid #e0e4f0;">
                              <p style="margin:0;font-size:12px;color:#aaa;">
                                © {DateTime.UtcNow.Year} StayEasy. All rights reserved.<br/>
                                This is an automated email — please do not reply directly.
                              </p>
                            </td>
                          </tr>

                        </table>
                      </td>
                    </tr>
                  </table>
                </body>
                </html>
                """;
        }
    }
}
