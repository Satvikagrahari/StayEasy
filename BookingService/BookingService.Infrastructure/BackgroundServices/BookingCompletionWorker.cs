using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using BookingService.Application.IntegrationEvents;
using BookingService.Infrastructure.Data;
using BookingService.Infrastructure.Messaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace BookingService.Infrastructure.BackgroundServices
{
    public class BookingCompletionWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BookingCompletionWorker> _logger;

        public BookingCompletionWorker(IServiceProvider serviceProvider, ILogger<BookingCompletionWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Booking Completion Worker is starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessCompletedBookingsAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred processing completed bookings.");
                }

                // Wait for an hour before running again (or customize as needed)
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }

        private async Task ProcessCompletedBookingsAsync(CancellationToken stoppingToken)
        {
            using var scope = _serviceProvider.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            var publisher = scope.ServiceProvider.GetRequiredService<IBookingPublisher>();

            var now = DateTime.UtcNow;

            var bookingsToComplete = await db.Bookings
                .Include(b => b.BookingItems)
                .Where(b => b.Status == "Confirmed" && b.CheckOut < now)
                .ToListAsync(stoppingToken);

            if (!bookingsToComplete.Any())
                return;

            _logger.LogInformation("Found {Count} bookings to complete.", bookingsToComplete.Count);

            foreach (var booking in bookingsToComplete)
            {
                booking.Status = "Completed";

                var completedEvent = new BookingCompletedIntegrationEvent
                {
                    BookingId = booking.Id,
                    BookingItems = booking.BookingItems.Select(i => new BookingItemDto
                    {
                        RoomTypeId = i.RoomTypeId,
                        Nights = i.Nights
                    }).ToList()
                };

                await publisher.PublishEventAsync(completedEvent, "booking.completed");
            }

            await db.SaveChangesAsync(stoppingToken);
            _logger.LogInformation("Successfully processed and completed {Count} bookings.", bookingsToComplete.Count);
        }
    }
}
