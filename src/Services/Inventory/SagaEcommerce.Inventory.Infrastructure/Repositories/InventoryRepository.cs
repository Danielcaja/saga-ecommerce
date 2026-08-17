using Microsoft.EntityFrameworkCore;
using SagaEcommerce.Inventory.Domain.Entities;
using SagaEcommerce.Inventory.Domain.Repositories;
using SagaEcommerce.Inventory.Infrastructure.Data;

namespace SagaEcommerce.Inventory.Infrastructure.Repositories;

public class InventoryRepository(InventoryDbContext context) : IInventoryRepository
{
    public async Task<InventoryItem?> GetByProductIdAsync(Guid productId)
    {
        return await context.InventoryItems
            .FirstOrDefaultAsync(x => x.ProductId == productId);
    }

    public async Task<IEnumerable<InventoryItem>> GetAllAsync()
    {
        return await context.InventoryItems
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task AddAsync(InventoryItem item)
    {
        await context.InventoryItems.AddAsync(item);
    }

    public async Task UpdateAsync(InventoryItem item)
    {
        context.InventoryItems.Update(item);
        await Task.CompletedTask;
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await context.SaveChangesAsync() > 0;
    }
}
