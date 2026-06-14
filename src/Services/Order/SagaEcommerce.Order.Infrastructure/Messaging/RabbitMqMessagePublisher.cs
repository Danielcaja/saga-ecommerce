using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SagaEcommerce.Order.Application.Interfaces;

namespace SagaEcommerce.Order.Infrastructure.Messaging;

public class RabbitMqMessagePublisher(IOptions<RabbitMqSettings> settings) : IOrderEventPublisher
{
    private readonly RabbitMqSettings _settings = settings.Value;

    public async Task PublishAsync<T>(T @event, string routingKey) where T : class
    {
        var factory = new ConnectionFactory
        {
            HostName = _settings.HostName,
            Port = _settings.Port,
            UserName = _settings.UserName,
            Password = _settings.Password
        };

        // Establishes the connection and creates the channel asynchronously (RabbitMQ.Client v7+)
        using var connection = await factory.CreateConnectionAsync();
        using var channel = await connection.CreateChannelAsync();

        // 1. Declare the Topic Exchange (standard in Event-driven / Saga architectures)
        await channel.ExchangeDeclareAsync(
            exchange: _settings.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null
        );

        // 2. Create and bind a test queue to ensure messages aren't lost and remain visible in RabbitMQ UI
        var queueName = "order-created-queue";
        await channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: null
        );

        await channel.QueueBindAsync(
            queue: queueName,
            exchange: _settings.ExchangeName,
            routingKey: routingKey
        );

        // 3. Serialize event to JSON
        var json = JsonSerializer.Serialize(@event, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        var body = Encoding.UTF8.GetBytes(json);

        // 4. Configure message persistence (BasicProperties in v7+)
        var properties = new BasicProperties
        {
            Persistent = true
        };

        // 5. Publish to exchange
        await channel.BasicPublishAsync(
            exchange: _settings.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body
        );
    }
}
