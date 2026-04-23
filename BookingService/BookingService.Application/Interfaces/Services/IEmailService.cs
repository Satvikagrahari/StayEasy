using System;
using System.Threading.Tasks;

namespace BookingService.Application.Interfaces.Services
{
    public interface IEmailService
    {
        Task SendBookingConfirmationAsync(string toEmail, Guid bookingId, decimal totalAmount, DateTime bookingDate);
    }
}
