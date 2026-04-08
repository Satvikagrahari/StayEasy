using BookingService.Application.DTOs.Request;
using BookingService.Application.Interfaces.Services;
using BookingService.Domain.Entities;
using BookingService.Infrastructure.Data;
using BookingService.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace BookingService.Infrastructure.Services
{
    public class BookingService : IBookingService
    {
        private readonly BookingDbContext _context;

        public BookingService(BookingDbContext context)
        {
            _context = context;
        }
        private readonly RabbitMQPublisher _publisher;

        public BookingService(BookingDbContext context, RabbitMQPublisher publisher)
        {
            _context = context;
            _publisher = publisher;
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
            await _context.SaveChangesAsync();

            _publisher.PublishBookingCreated(new
            {
                booking.Id,
                booking.UserId,
                booking.HotelId,
                booking.TotalPrice
            });
        }

        public async Task<List<Booking>> GetUserBookingsAsync(Guid userId)
        {
            return await _context.Bookings
                .Where(x => x.UserId == userId)
                .ToListAsync();
        }
        public async Task<Guid> CheckoutAsync(Guid userId)
        {
            var cart = await _context.Carts
                .Include(c => c.Items)
                .FirstOrDefaultAsync(c => c.UserId == userId && c.Status == "Active");

            if (cart == null || !cart.Items.Any())
                throw new Exception("Cart is empty");

            var total = cart.Items.Sum(i => i.PriceSnapshot);

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TotalAmount = total,
                Status = "Pending",
                BookingDate = DateTime.UtcNow
            };

            await _context.Bookings.AddAsync(booking);

            // OPTIONAL: create BookingItems later

            cart.Status = "CheckedOut";

            await _context.SaveChangesAsync();

            return booking.Id;
        }
    }
}
