using Microsoft.EntityFrameworkCore;
using SagaEcommerce.Inventory.Application.DTOs;
using SagaEcommerce.Inventory.Application.Interfaces;
using SagaEcommerce.Inventory.Domain.Repositories;

namespace SagaEcommerce.Inventory.Application.Services;

public class InventoryAppService(IInventoryRepository repository) : IInventoryAppService
{
    public async Task<InventoryItemDto?> GetByProductIdAsync(Guid productId)
    {
        var item = await repository.GetByProductIdAsync(productId);
        return item == null ? null : new InventoryItemDto(item.Id, item.ProductId, item.AvailableQuantity);
    }

    public async Task<bool> ReserveStockAsync(Guid productId, int quantity)
    {
        const int maxRetries = 3;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                var item = await repository.GetByProductIdAsync(productId);
                if (item == null || !item.IsQuantityAvailable(quantity))
                {
                    return false;
                }

                item.Reserve(quantity);
                await repository.UpdateAsync(item);
                return await repository.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException) when (attempt < maxRetries)
            {
                // Retry if a concurrent transaction updated the item between read and save
                await Task.Delay(50 * attempt);
            }
        }

        return false;
    }

    public async Task<IEnumerable<InventoryItemDto>> GetAllAsync()
    {
        var items = await repository.GetAllAsync();
        return items.Select(item => new InventoryItemDto(item.Id, item.ProductId, item.AvailableQuantity));
    }
}
