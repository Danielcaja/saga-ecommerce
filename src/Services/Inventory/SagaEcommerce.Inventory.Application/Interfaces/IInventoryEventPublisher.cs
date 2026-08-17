namespace SagaEcommerce.Inventory.Application.Interfaces;

public interface IInventoryEventPublisher
{
    Task PublishAsync<T>(T @event, string routingKey) where T : class;
}
