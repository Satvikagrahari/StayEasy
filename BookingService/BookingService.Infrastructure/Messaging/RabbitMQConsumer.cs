using BookingService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;

namespace BookingService.Infrastructure.Messaging
{
    public class RabbitMQConsumer : IDisposable
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConnection _connection;
        private readonly IModel _channel;

        public RabbitMQConsumer(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;

            var factory = new ConnectionFactory() { HostName = "localhost" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();

            _channel.QueueDeclare(
                queue: "booking_queue",
                durable: false,
                exclusive: false,
                autoDelete: false);
        }

        public void StartListening()
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var message = Encoding.UTF8.GetString(body);
                    var bookingData = JsonSerializer.Deserialize<JsonElement>(message);

                    // Extract booking ID
                    if (bookingData.TryGetProperty("Id", out var idElement))
                    {
                        var bookingId = Guid.Parse(idElement.GetString());

                        // Create a new scope for database access
                        using (var scope = _serviceProvider.CreateScope())
                        {
                            var context = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
                            var booking = await context.Bookings.FindAsync(bookingId);
                            if (booking != null)
                            {
                                booking.Status = "Confirmed";
                                await context.SaveChangesAsync();
                                Console.WriteLine($"✓ Booking {bookingId} confirmed");
                            }
                        }
                    }

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Error processing message: {ex.Message}");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
                }
            };

            _channel.BasicConsume(queue: "booking_queue", autoAck: false, consumer: consumer);
            Console.WriteLine("Listening for booking messages...");
        }

        public void Dispose()
        {
            _channel?.Close();
            _connection?.Close();
        }
    }
}