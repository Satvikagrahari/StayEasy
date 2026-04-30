using BookingService.Application.DTOs.Request;
using BookingService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Application.Interfaces.Services
{
    public interface IBookingService
    {
        //Task CreateBookingAsync(Guid userId, CreateBookingRequest request);

        Task<List<Booking>> GetAllBookingsAsync();
        Task<List<Booking>> GetUserBookingsAsync(Guid userId);
        Task<Guid> CheckoutAsync(Guid userId, string? promoCode = null);
        Task<List<Booking>> GetPendingBookingsAsync();
        Task<List<Booking>> GetConfirmedBookingsAsync();
        Task<bool> UpdateBookingStatusAsync(Guid bookingId, BookingStatus status);
        Task<string> CreateRazorpayOrderAsync(Guid bookingId);
        Task<bool> VerifyRazorpayPaymentAsync(RazorpayPaymentVerificationRequest request);
        Task<bool> CancelBookingAsync(Guid bookingId, Guid userId);
        Task<bool> RequestRefundAsync(Guid bookingId, Guid userId);
        Task<bool> ApproveRefundAsync(Guid bookingId);
        Task<byte[]?> GetInvoiceAsync(Guid bookingId, Guid userId);
        Task<byte[]> GetAdminReportAsync();
    }
}
