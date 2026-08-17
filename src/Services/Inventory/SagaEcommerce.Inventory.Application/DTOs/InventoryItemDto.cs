namespace SagaEcommerce.Inventory.Application.DTOs;

public record InventoryItemDto(Guid Id, Guid ProductId, int AvailableQuantity);
