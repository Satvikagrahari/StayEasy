using BookingService.Application.DTOs.Request;
using BookingService.Domain.Entities;
using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Application.Interfaces.Services
{
    public interface IBookingService
    {
        Task CreateBookingAsync(Guid userId, CreateBookingRequest request);

        Task<List<Booking>> GetUserBookingsAsync(Guid userId);
    }
}
