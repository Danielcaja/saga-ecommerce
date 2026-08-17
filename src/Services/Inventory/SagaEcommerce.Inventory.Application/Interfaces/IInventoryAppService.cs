using SagaEcommerce.Inventory.Application.DTOs;

namespace SagaEcommerce.Inventory.Application.Interfaces;

public interface IInventoryAppService
{
    Task<InventoryItemDto?> GetByProductIdAsync(Guid productId);
    Task<bool> ReserveStockAsync(Guid productId, int quantity);
    Task<IEnumerable<InventoryItemDto>> GetAllAsync();
}
