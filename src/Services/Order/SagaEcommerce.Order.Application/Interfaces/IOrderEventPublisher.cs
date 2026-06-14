namespace SagaEcommerce.Order.Application.Interfaces;

public interface IOrderEventPublisher
{
    Task PublishAsync<T>(T @event, string routingKey) where T : class;
}
