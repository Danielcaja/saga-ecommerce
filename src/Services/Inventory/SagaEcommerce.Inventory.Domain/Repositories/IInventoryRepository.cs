using SagaEcommerce.Inventory.Domain.Entities;

namespace SagaEcommerce.Inventory.Domain.Repositories;

public interface IInventoryRepository
{
    Task<InventoryItem?> GetByProductIdAsync(Guid productId);
    Task<IEnumerable<InventoryItem>> GetAllAsync();
    Task AddAsync(InventoryItem item);
    Task UpdateAsync(InventoryItem item);
    Task<bool> SaveChangesAsync();
}
