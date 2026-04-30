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
        Task<Guid> CheckoutAsync(Guid userId);
        Task<List<Booking>> GetPendingBookingsAsync();
        Task<List<Booking>> GetConfirmedBookingsAsync();
        Task<bool> UpdateBookingStatusAsync(Guid bookingId, BookingStatus status);
        Task SimulatePaymentAsync(Guid bookingId, bool isSuccess);
        Task<bool> CancelBookingAsync(Guid bookingId, Guid userId);
        Task<bool> RequestRefundAsync(Guid bookingId, Guid userId);
        Task<bool> ApproveRefundAsync(Guid bookingId);
    }
}
