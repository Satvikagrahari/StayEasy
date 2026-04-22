using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using RabbitMQ.Client;
using Microsoft.Extensions.Logging;

namespace BookingService.Infrastructure.Messaging
{
    public interface IBookingPublisher
    {
        Task PublishEventAsync<T>(T @event, string routingKey);
    }

    public class RabbitMQPublisher : IBookingPublisher, IDisposable
    {
        private readonly IConnection _connection;
        private readonly IModel _channel;
        private const string ExchangeName = "hotel_booking_exchange";

        public RabbitMQPublisher()
        {
            var factory = new ConnectionFactory { HostName = "localhost" };
            _connection = factory.CreateConnection();
            _channel = _connection.CreateModel();
            
            _channel.ExchangeDeclare(exchange: ExchangeName, type: ExchangeType.Topic, durable: true);
        }

        public Task PublishEventAsync<T>(T @event, string routingKey)
        {
            var json = JsonSerializer.Serialize(@event);
            var body = Encoding.UTF8.GetBytes(json);

            // Publish message to the topic exchange
            _channel.BasicPublish(exchange: ExchangeName, routingKey: routingKey, basicProperties: null, body: body);
            
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}