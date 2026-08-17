using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;

namespace SagaEcommerce.Inventory.Infrastructure.Messaging;

public class RabbitMqPersistentConnection : IRabbitMqPersistentConnection
{
    private readonly ConnectionFactory _connectionFactory;
    private readonly ILogger<RabbitMqPersistentConnection> _logger;
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private IConnection? _connection;
    private bool _disposed;

    public RabbitMqPersistentConnection(
        IOptions<RabbitMqSettings> settings,
        ILogger<RabbitMqPersistentConnection> logger)
    {
        _logger = logger;
        var s = settings.Value;
        _connectionFactory = new ConnectionFactory
        {
            HostName = s.HostName,
            Port = s.Port,
            UserName = s.UserName,
            Password = s.Password
        };
    }

    public bool IsConnected => _connection is { IsOpen: true } && !_disposed;

    public async Task<bool> TryConnectAsync(CancellationToken cancellationToken = default)
    {
        if (IsConnected) return true;

        await _semaphore.WaitAsync(cancellationToken);
        try
        {
            if (IsConnected) return true;

            _logger.LogInformation("Establishing persistent RabbitMQ connection...");
            _connection = await _connectionFactory.CreateConnectionAsync(cancellationToken);

            if (IsConnected)
            {
                _logger.LogInformation("Persistent RabbitMQ connection established successfully.");
                return true;
            }

            _logger.LogWarning("Failed to establish RabbitMQ connection.");
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Fatal error connecting to RabbitMQ.");
            return false;
        }
        finally
        {
            _semaphore.Release();
        }
    }

    public async Task<IChannel> CreateChannelAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
        {
            var connected = await TryConnectAsync(cancellationToken);
            if (!connected || _connection == null)
            {
                throw new InvalidOperationException("No RabbitMQ connection available to create channel.");
            }
        }

        return await _connection!.CreateChannelAsync(cancellationToken: cancellationToken);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            _connection?.Dispose();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing RabbitMQ connection.");
        }
    }
}
