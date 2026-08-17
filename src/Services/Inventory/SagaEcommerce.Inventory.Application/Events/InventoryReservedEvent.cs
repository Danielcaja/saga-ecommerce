namespace SagaEcommerce.Inventory.Application.Events;

public record InventoryReservedEvent(Guid OrderId, Guid ProductId, int Quantity, DateTime ReservedAt);
