using BookingService.Application.DTOs.Request;
using BookingService.Application.IntegrationEvents;
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
        private readonly IBookingPublisher _publisher;

        public BookingService(BookingDbContext context, IBookingPublisher publisher)
        {   
            _context = context;
            _publisher = publisher;
        }
        

        public async Task<List<Booking>> GetAllBookingsAsync()
        {
            return await _context.Bookings
                .Include(b => b.BookingItems)
                .OrderByDescending(b => b.BookingDate)
                .ToListAsync();
        }

        public async Task<List<Booking>> GetUserBookingsAsync(Guid userId)
        {
            return await _context.Bookings
                .Include(b => b.BookingItems)
                .Where(x => x.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
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
                Status = BookingStatus.Pending,
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

            //_publisher.PublishBookingCreated(new
            //{
            //    booking.Id,
            //    booking.UserId,
            //    booking.TotalAmount,
            //    Items = booking.BookingItems.Select(i => new
            //    {
            //        i.BookingItemId,
            //        i.HotelId,
            //        i.RoomTypeId,
            //        i.CheckInDate,
            //        i.CheckOutDate,
            //        i.Nights,
            //        i.Subtotal
            //    })
            //});

            var bookingCreatedEvent = new BookingCreatedIntegrationEvent
            {
                BookingId = booking.Id,
                UserId = booking.UserId,
                TotalAmount = booking.TotalAmount,
                Status = booking.Status.ToString(),
                CreatedAt = DateTime.UtcNow,
                BookingItems = bookingItems.Select(i => new BookingItemDto
                {
                    RoomTypeId = i.RoomTypeId,
                    Nights = i.Nights
                }).ToList()
            };
            
            await _publisher.PublishEventAsync(bookingCreatedEvent, "booking.created");

            return booking.Id;
        }

        public async Task<List<Booking>> GetPendingBookingsAsync()
        {
            return await _context.Bookings
                .Include(b => b.BookingItems)
                .Where(b => b.Status == BookingStatus.Pending)
                .ToListAsync();
        }

        public async Task<List<Booking>> GetConfirmedBookingsAsync()
        {
            return await _context.Bookings
                .Include(b => b.BookingItems)
                .Where(b => b.Status == BookingStatus.Confirmed)
                .ToListAsync();
        }

        public async Task<bool> UpdateBookingStatusAsync(Guid bookingId, BookingStatus status)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null)
                return false;

            booking.Status = status;
            await _context.SaveChangesAsync();

            return true;
        }
        public async Task SimulatePaymentAsync(Guid bookingId, bool isSuccess)
        {
            var paymentEvent = new PaymentProcessedIntegrationEvent
            {
                BookingId = bookingId,
                IsSuccess = isSuccess,
                ProcessedAt = DateTime.UtcNow
            };

            await _publisher.PublishEventAsync(paymentEvent, "payment.processed");
        }

        public async Task<bool> CancelBookingAsync(Guid bookingId, Guid userId)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingItems)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);
            
            if (booking == null) return false;
            if (booking.Status == BookingStatus.Cancelled) return true;

            booking.Status = BookingStatus.Cancelled;
            booking.CancellationDate = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var cancelEvent = new BookingCancelledIntegrationEvent
            {
                BookingId = booking.Id,
                BookingItems = booking.BookingItems.Select(i => new BookingItemDto
                {
                    RoomTypeId = i.RoomTypeId,
                    Nights = i.Nights
                }).ToList()
            };
            
            await _publisher.PublishEventAsync(cancelEvent, "booking.cancelled");

            return true;
        }

        public async Task<bool> RequestRefundAsync(Guid bookingId, Guid userId)
        {
            var booking = await _context.Bookings
                .Include(b => b.BookingItems)
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null) return false;
            if (booking.Status != BookingStatus.Cancelled) return false;

            // Business Logic: Guests may cancel up to 24 hours before check-in for a full refund.
            // We use the earliest check-in date among all items.
            var earliestCheckIn = booking.BookingItems.Min(i => i.CheckInDate);
            var cancellationTime = booking.CancellationDate ?? DateTime.UtcNow;

            var timeUntilCheckIn = earliestCheckIn - cancellationTime;

            if (timeUntilCheckIn.TotalHours < 24)
            {
                // Non-refundable according to policy, but admin can still process manually if configured.
                // For now, we'll allow requesting, but maybe flag it? 
                // The requirement says "Cancellations within 24 hours ... are non-refundable (configurable by Admin)".
                // I will allow requesting, but the Admin will see it's non-refundable.
                // Or I can block it here. Let's block it for now as per "non-refundable" rule.
                throw new Exception("Cancellation was made within 24 hours of check-in and is non-refundable.");
            }

            booking.Status = BookingStatus.RefundRequested;
            await _context.SaveChangesAsync();

            var refundRequestEvent = new RefundRequestedIntegrationEvent
            {
                BookingId = booking.Id,
                UserId = booking.UserId,
                RefundAmount = booking.TotalAmount,
                RequestedAt = DateTime.UtcNow
            };

            await _publisher.PublishEventAsync(refundRequestEvent, "booking.refund-requested");

            return true;
        }

        public async Task<bool> ApproveRefundAsync(Guid bookingId)
        {
            var booking = await _context.Bookings.FindAsync(bookingId);
            if (booking == null) return false;

            if (booking.Status != BookingStatus.RefundRequested)
                return false;

            booking.Status = BookingStatus.Refunded;
            await _context.SaveChangesAsync();

            var refundedEvent = new RefundedIntegrationEvent
            {
                BookingId = booking.Id,
                RefundedAmount = booking.TotalAmount,
                RefundedAt = DateTime.UtcNow
            };

            await _publisher.PublishEventAsync(refundedEvent, "booking.refunded");

            return true;
        }
    }
}
