namespace SagaEcommerce.Inventory.Application.Events;

public record InventoryRejectedEvent(Guid OrderId, Guid ProductId, int Quantity, string Reason, DateTime RejectedAt);
