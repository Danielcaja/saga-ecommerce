namespace SagaEcommerce.Order.Application.Events;

public record OrderCreatedEvent(Guid OrderId, Guid ClientId, decimal Total, DateTime CreatedAt);
