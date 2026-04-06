using BookingService.Domain.Entities;
using BookingService.Infrastructure.Data;
using BookingService.Application.Interfaces.Services;
using System;
using System.Collections.Generic;
using System.Text;
using BookingService.Application.DTOs.Request;
using Microsoft.EntityFrameworkCore;

namespace BookingService.Infrastructure.Services
{
    public class BookingService : IBookingService
    {
        private readonly BookingDbContext _context;

        public BookingService(BookingDbContext context)
        {
            _context = context;
        }

        public async Task CreateBookingAsync(Guid userId, CreateBookingRequest request)
        {
            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                HotelId = request.HotelId,
                CheckIn = request.CheckIn,
                CheckOut = request.CheckOut,
                Guests = request.Guests,
                TotalPrice = 1000, // temp logic
                Status = "Confirmed"
            };

            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Booking>> GetUserBookingsAsync(Guid userId)
        {
            return await _context.Bookings
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }
    }
}
