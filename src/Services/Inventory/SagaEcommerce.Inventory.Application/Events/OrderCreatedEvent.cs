namespace SagaEcommerce.Inventory.Application.Events;

public record OrderCreatedEvent(Guid OrderId, Guid ProductId, int Quantity, decimal Total, DateTime CreatedAt);
