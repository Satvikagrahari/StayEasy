using BookingService.Domain.Entities;
using System.Threading.Tasks;

namespace BookingService.Application.Interfaces.Services
{
    public interface IInvoiceService
    {
        Task<byte[]> GenerateInvoiceAsync(Booking booking, string userName, string hotelName);
        Task<byte[]> GenerateAdminReportAsync(List<Booking> bookings);
    }
}
