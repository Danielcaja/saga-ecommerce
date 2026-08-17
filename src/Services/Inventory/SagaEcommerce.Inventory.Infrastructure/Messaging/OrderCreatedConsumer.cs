using System.Text;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SagaEcommerce.Inventory.Application.Events;
using SagaEcommerce.Inventory.Application.Interfaces;

namespace SagaEcommerce.Inventory.Infrastructure.Messaging;

public class OrderCreatedConsumer : BackgroundService
{
    private readonly RabbitMqSettings _settings;
    private readonly IRabbitMqPersistentConnection _persistentConnection;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<OrderCreatedConsumer> _logger;
    private IChannel? _channel;

    public OrderCreatedConsumer(
        IOptions<RabbitMqSettings> settings,
        IRabbitMqPersistentConnection persistentConnection,
        IServiceProvider serviceProvider,
        ILogger<OrderCreatedConsumer> logger)
    {
        _settings = settings.Value;
        _persistentConnection = persistentConnection;
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested && !_persistentConnection.IsConnected)
        {
            _logger.LogInformation("Waiting for persistent RabbitMQ connection...");
            var connected = await _persistentConnection.TryConnectAsync(stoppingToken);
            if (!connected)
            {
                _logger.LogWarning("Failed to connect to RabbitMQ. Retrying in 5 seconds...");
                await Task.Delay(5000, stoppingToken);
            }
        }

        if (stoppingToken.IsCancellationRequested || !_persistentConnection.IsConnected)
            return;

        _channel = await _persistentConnection.CreateChannelAsync(stoppingToken);

        // 1. Declare Dead Letter Exchange (DLX) and Dead Letter Queue (DLQ)
        var dlxExchange = "order.dlx";
        var dlqQueue = "inventory-order-created-dlq";

        await _channel.ExchangeDeclareAsync(
            exchange: dlxExchange,
            type: ExchangeType.Direct,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken
        );

        await _channel.QueueDeclareAsync(
            queue: dlqQueue,
            durable: true,
            exclusive: false,
            autoDelete: false,
            cancellationToken: stoppingToken
        );

        await _channel.QueueBindAsync(
            queue: dlqQueue,
            exchange: dlxExchange,
            routingKey: "order.created.dlq",
            cancellationToken: stoppingToken
        );

        // 2. Declare main Exchange
        await _channel.ExchangeDeclareAsync(
            exchange: _settings.ExchangeName,
            type: ExchangeType.Topic,
            durable: true,
            autoDelete: false,
            cancellationToken: stoppingToken
        );

        // 3. Declare main queue with DLX arguments
        var queueName = "inventory-order-created-queue";
        var queueArguments = new Dictionary<string, object?>
        {
            { "x-dead-letter-exchange", dlxExchange },
            { "x-dead-letter-routing-key", "order.created.dlq" }
        };

        await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: queueArguments,
            cancellationToken: stoppingToken
        );

        await _channel.QueueBindAsync(
            queue: queueName,
            exchange: _settings.ExchangeName,
            routingKey: "order.created",
            cancellationToken: stoppingToken
        );

        _logger.LogInformation("Waiting for OrderCreatedEvents on queue '{Queue}' (DLQ configured)...", queueName);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += async (sender, eventArgs) =>
        {
            var body = eventArgs.Body.ToArray();
            var message = Encoding.UTF8.GetString(body);

            try
            {
                _logger.LogInformation("Received OrderCreatedEvent message: {Message}", message);

                var orderCreatedEvent = JsonSerializer.Deserialize<OrderCreatedEvent>(message, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (orderCreatedEvent != null)
                {
                    using var scope = _serviceProvider.CreateScope();
                    var inventoryAppService = scope.ServiceProvider.GetRequiredService<IInventoryAppService>();
                    var eventPublisher = scope.ServiceProvider.GetRequiredService<IInventoryEventPublisher>();

                    _logger.LogInformation("Checking stock for Product: {ProductId}, Qty: {Quantity}", 
                        orderCreatedEvent.ProductId, orderCreatedEvent.Quantity);

                    var success = await inventoryAppService.ReserveStockAsync(orderCreatedEvent.ProductId, orderCreatedEvent.Quantity);

                    if (success)
                    {
                        _logger.LogInformation("Stock successfully reserved for Order: {OrderId}", orderCreatedEvent.OrderId);
                        
                        var reservedEvent = new InventoryReservedEvent(
                            orderCreatedEvent.OrderId, 
                            orderCreatedEvent.ProductId, 
                            orderCreatedEvent.Quantity, 
                            DateTime.UtcNow
                        );

                        await eventPublisher.PublishAsync(reservedEvent, "inventory.reserved");
                    }
                    else
                    {
                        _logger.LogWarning("Insufficient stock or product not found. Rejecting reservation for Order: {OrderId}", orderCreatedEvent.OrderId);

                        var rejectedEvent = new InventoryRejectedEvent(
                            orderCreatedEvent.OrderId,
                            orderCreatedEvent.ProductId,
                            orderCreatedEvent.Quantity,
                            "Insufficient stock or product not found.",
                            DateTime.UtcNow
                        );

                        await eventPublisher.PublishAsync(rejectedEvent, "inventory.rejected");
                    }
                }

                // Acknowledge the message
                await _channel.BasicAckAsync(eventArgs.DeliveryTag, multiple: false);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing OrderCreatedEvent message. Nacking to DLQ.");
                // Nack message with requeue=false so it is sent to Dead Letter Exchange
                await _channel.BasicNackAsync(eventArgs.DeliveryTag, multiple: false, requeue: false);
            }
        };

        await _channel.BasicConsumeAsync(
            queue: queueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: stoppingToken
        );

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (TaskCanceledException)
        {
            // Normal shutdown
        }
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Stopping OrderCreatedConsumer...");
        
        if (_channel != null)
        {
            await _channel.CloseAsync(cancellationToken);
        }

        await base.StopAsync(cancellationToken);
    }
}
