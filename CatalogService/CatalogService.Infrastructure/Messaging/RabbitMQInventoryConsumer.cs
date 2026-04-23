using CatalogService.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

namespace CatalogService.Infrastructure.Messaging
{
    public class BookingItemDto
    {
        public Guid RoomTypeId { get; set; }
        public int Nights { get; set; }
    }

    public class BookingCreatedIntegrationEvent
    {
        public Guid BookingId { get; set; }
        public Guid UserId { get; set; }
        public decimal TotalAmount { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<BookingItemDto> BookingItems { get; set; } = new();
    }

    public class BookingCancelledIntegrationEvent
    {
        public Guid BookingId { get; set; }
        public List<BookingItemDto> BookingItems { get; set; } = new();
    }

    public class BookingCompletedIntegrationEvent
    {
        public Guid BookingId { get; set; }
        public List<BookingItemDto> BookingItems { get; set; } = new();
    }

    public class RabbitMQInventoryConsumer : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RabbitMQInventoryConsumer> _logger;
        private IConnection _connection;
        private IModel _channel;

        private const string ExchangeName = "hotel_booking_exchange";
        private const string QueueName = "catalog_inventory_queue";

        public RabbitMQInventoryConsumer(IServiceProvider serviceProvider, ILogger<RabbitMQInventoryConsumer> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            
            var factory = new ConnectionFactory { HostName = "localhost", DispatchConsumersAsync = true };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            _channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Topic, durable: true);
            _channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false);
            
            _channel.QueueBind(queue: QueueName, exchange: ExchangeName, routingKey: "booking.created");
            _channel.QueueBind(queue: QueueName, exchange: ExchangeName, routingKey: "booking.cancelled");
            _channel.QueueBind(queue: QueueName, exchange: ExchangeName, routingKey: "booking.completed");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                var routingKey = ea.RoutingKey;
                var message = Encoding.UTF8.GetString(ea.Body.ToArray());

                try
                {
                    using var scope = _serviceProvider.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

                    if (routingKey == "booking.created")
                    {
                        var ev = JsonSerializer.Deserialize<BookingCreatedIntegrationEvent>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (ev != null && ev.BookingItems != null)
                        {
                            foreach (var item in ev.BookingItems)
                            {
                                var roomType = await db.RoomTypes.FindAsync(new object[] { item.RoomTypeId }, stoppingToken);
                                if (roomType != null)
                                {
                                    // Subtract 1 available room per item booked
                                    roomType.AvailableRooms = Math.Max(0, roomType.AvailableRooms - 1);
                                }
                            }
                            await db.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation("Inventory decreased for booking {BookingId}", ev.BookingId);
                        }
                    }
                    else if (routingKey == "booking.cancelled")
                    {
                        var ev = JsonSerializer.Deserialize<BookingCancelledIntegrationEvent>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (ev != null && ev.BookingItems != null)
                        {
                            foreach (var item in ev.BookingItems)
                            {
                                var roomType = await db.RoomTypes.FindAsync(new object[] { item.RoomTypeId }, stoppingToken);
                                if (roomType != null)
                                {
                                    roomType.AvailableRooms += 1;
                                }
                            }
                            await db.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation("Inventory increased for cancelled booking {BookingId}", ev.BookingId);
                        }
                    }
                    else if (routingKey == "booking.completed")
                    {
                        var ev = JsonSerializer.Deserialize<BookingCompletedIntegrationEvent>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                        if (ev != null && ev.BookingItems != null)
                        {
                            foreach (var item in ev.BookingItems)
                            {
                                var roomType = await db.RoomTypes.FindAsync(new object[] { item.RoomTypeId }, stoppingToken);
                                if (roomType != null)
                                {
                                    roomType.AvailableRooms += 1;
                                }
                            }
                            await db.SaveChangesAsync(stoppingToken);
                            _logger.LogInformation("Inventory restored for completed booking {BookingId}", ev.BookingId);
                        }
                    }

                    _channel.BasicAck(ea.DeliveryTag, false);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing inventory message for routing key {RoutingKey}", routingKey);
                    // Decide whether to nack and requeue, but for simplicity we won't requeue here
                    _channel.BasicNack(ea.DeliveryTag, false, false);
                }
            };

            _channel.BasicConsume(queue: QueueName, autoAck: false, consumer: consumer);
            
            return Task.CompletedTask;
        }

        public override void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
            base.Dispose();
        }
    }
}
