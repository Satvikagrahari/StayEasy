using BookingService.Application.DTOs.Request;
using BookingService.Application.Interfaces.Services;
using BookingService.Domain.Entities;
using BookingService.Infrastructure.Data;
using BookingService.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BookingService.Infrastructure.Services
{
    public class BookingService : IBookingService
    {
        private readonly BookingDbContext _context;
        private readonly RabbitMQPublisher _publisher;

        public BookingService(BookingDbContext context, RabbitMQPublisher publisher)
        {
            _context = context;
            _publisher = publisher;
        }
        //public async Task CreateBookingAsync(Guid userId, CreateBookingRequest request)
        //{
        //    var booking = new Booking
        //    {
        //        Id = Guid.NewGuid(),
        //        UserId = userId,
        //        HotelId = request.HotelId,
        //        CheckIn = request.CheckIn,
        //        CheckOut = request.CheckOut,
        //        Guests = request.Guests,
        //        TotalPrice = 1000, // temp logic
        //        Status = "Confirmed"
        //    };

        //    await _context.Bookings.AddAsync(booking);
        //    await _context.SaveChangesAsync();


        //    _publisher.PublishBookingCreated(new
        //    {
        //        booking.Id,
        //        booking.UserId,
        //        booking.HotelId,
        //        booking.TotalPrice
        //    });
        //}

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

            var total = cart.Items.Sum(i =>
                (i.CheckOutDate - i.CheckInDate).Days * i.PriceSnapshot
             );

            var booking = new Booking
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TotalAmount = total,
                Status = "Pending",
                BookingDate = DateTime.UtcNow,
                BookingItems = new List<BookingItem>()
            };

            await _context.Bookings.AddAsync(booking);
            await _context.SaveChangesAsync();

            // OPTIONAL: create BookingItems later

            // Create BookingItems from cart items
            var bookingItems = cart.Items.Select(cartItem => new BookingItem
            {
                BookingItemId = Guid.NewGuid(),
                BookingId = booking.Id,
                HotelId = cartItem.HotelId,
                RoomTypeId = cartItem.RoomTypeId,
                CheckInDate = cartItem.CheckInDate,
                CheckOutDate = cartItem.CheckOutDate,
                Nights = (cartItem.CheckOutDate - cartItem.CheckInDate).Days,
                PricePerNight = cartItem.PriceSnapshot,
                Subtotal = (cartItem.CheckOutDate - cartItem.CheckInDate).Days * cartItem.PriceSnapshot
            }).ToList();

            await _context.BookingItems.AddRangeAsync(bookingItems);

            cart.Status = "CheckedOut";
            await _context.SaveChangesAsync();

            booking.BookingItems = bookingItems;

            _publisher.PublishBookingCreated(new
            {
                booking.Id,
                booking.UserId,
                booking.TotalAmount,
                Items = booking.BookingItems.Select(i => new
                {
                    i.BookingItemId,
                    i.HotelId,
                    i.RoomTypeId,
                    i.CheckInDate,
                    i.CheckOutDate,
                    i.Nights,
                    i.PricePerNight,
                    i.Subtotal
                })
            });

            return booking.Id;
        }

        public async Task<List<Booking>> GetPendingBookingsAsync()
        {
            return await _context.Bookings
                .Where(b => b.Status == "Pending")
                .ToListAsync();
        }

        public async Task<List<Booking>> GetConfirmedBookingsAsync()
        {
            return await _context.Bookings
                .Where(b => b.Status == "Confirmed")
                .ToListAsync();
        }

        public async Task<bool> UpdateBookingStatusAsync(Guid bookingId, string status)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
                return false;

            booking.Status = status;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}
