using System;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using BookingService.Application.IntegrationEvents;
using BookingService.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace BookingService.Infrastructure.Messaging
{
    public class RabbitMQConsumerService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RabbitMQConsumerService> _logger;
        private IConnection _connection;
        private IModel _channel;

        private const string ExchangeName = "hotel_booking_exchange";
        private const string QueueName = "booking_service_queue";
        private const string RoutingKey = "payment.processed";

        public RabbitMQConsumerService(IServiceProvider serviceProvider, ILogger<RabbitMQConsumerService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            
            var factory = new ConnectionFactory { HostName = "localhost", DispatchConsumersAsync = true };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            _channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Topic, durable: true);
            _channel.QueueDeclare(queue: QueueName, durable: true, exclusive: false, autoDelete: false);
            _channel.QueueBind(queue: QueueName, exchange: ExchangeName, routingKey: RoutingKey);
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var consumer = new AsyncEventingBasicConsumer(_channel);

            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var message = Encoding.UTF8.GetString(ea.Body.ToArray());
                    var paymentEvent = JsonSerializer.Deserialize<PaymentProcessedIntegrationEvent>(message, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
                    
                    if (paymentEvent != null && paymentEvent.BookingId != Guid.Empty)
                    {
                        using var scope = _serviceProvider.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
                        var booking = await db.Bookings.FindAsync(new object[] { paymentEvent.BookingId }, stoppingToken);

                        if (booking != null)
                        {
                            if (paymentEvent.IsSuccess)
                            {
                                booking.Status = "Confirmed";
                                _logger.LogInformation("✓ Payment Successful. Booking {BookingId} is Confirmed.", paymentEvent.BookingId);
                            }
                            else
                            {
                                booking.Status = "Failed";
                                _logger.LogInformation("❌ Payment Failed. Booking {BookingId} is Failed.", paymentEvent.BookingId);
                            }
                            
                            await db.SaveChangesAsync(stoppingToken);
                            _channel.BasicAck(ea.DeliveryTag, false);
                        }
                        else
                        {
                            _logger.LogWarning("Booking {BookingId} not found. Dropping message.", paymentEvent.BookingId);
                            _channel.BasicNack(ea.DeliveryTag, false, false);
                        }
                    }
                    else
                    {
                        _logger.LogWarning("Received invalid payment message payload.");
                        _channel.BasicNack(ea.DeliveryTag, false, false);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing payment message");
                    _channel.BasicNack(ea.DeliveryTag, false, true);
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
