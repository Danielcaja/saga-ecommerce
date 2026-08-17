using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using SagaEcommerce.Inventory.Application.Interfaces;

namespace SagaEcommerce.Inventory.Infrastructure.Messaging;

public class RabbitMqMessagePublisher(
    IRabbitMqPersistentConnection persistentConnection,
    IOptions<RabbitMqSettings> settings) : IInventoryEventPublisher
{
    private readonly RabbitMqSettings _settings = settings.Value;

    public async Task PublishAsync<T>(T @event, string routingKey) where T : class
    {
        if (!persistentConnection.IsConnected)
        {
            await persistentConnection.TryConnectAsync();
        }

        using var channel = await persistentConnection.CreateChannelAsync();

        // 1. Declare the Exchange
        await channel.ExchangeDeclareAsync(
            exchange: _settings.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            arguments: null
        );

        // 2. Serialize event to JSON
        var json = JsonSerializer.Serialize(@event, new JsonSerializerOptions
        {
            WriteIndented = true
        });
        
        var body = Encoding.UTF8.GetBytes(json);

        // 3. Configure message persistence
        var properties = new BasicProperties
        {
            Persistent = true
        };

        // 4. Publish to exchange
        await channel.BasicPublishAsync(
            exchange: _settings.ExchangeName,
            routingKey: routingKey,
            mandatory: false,
            basicProperties: properties,
            body: body
        );
    }
}
