using RabbitMQ.Client;

namespace SagaEcommerce.Inventory.Infrastructure.Messaging;

public interface IRabbitMqPersistentConnection : IDisposable
{
    bool IsConnected { get; }
    Task<bool> TryConnectAsync(CancellationToken cancellationToken = default);
    Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default);
}
