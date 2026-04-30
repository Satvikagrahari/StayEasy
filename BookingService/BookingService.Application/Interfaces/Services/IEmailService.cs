using System;
using System.Threading.Tasks;

namespace BookingService.Application.Interfaces.Services
{
    public interface IEmailService
    {
        Task SendBookingConfirmationAsync(
            string toEmail,
            string userName,
            string hotelName,
            Guid bookingId,
            decimal billAmount,
            DateTime bookingDate,
            byte[]? attachment = null,
            string? attachmentName = null);
    }
}
