using Microsoft.EntityFrameworkCore;
using SagaEcommerce.Inventory.Domain.Entities;

namespace SagaEcommerce.Inventory.Infrastructure.Data;

public static class InventoryDbSeeder
{
    public static async Task SeedAsync(InventoryDbContext context)
    {
        // Apply migrations automatically
        await context.Database.MigrateAsync();

        var productAId = Guid.Parse("a612c2a1-0578-4e3d-ba94-595825c7a147");
        var productBId = Guid.Parse("b819cb79-17c7-4b29-b8a2-083fe1c12404");

        if (!await context.InventoryItems.AnyAsync(x => x.ProductId == productAId))
        {
            context.InventoryItems.Add(new InventoryItem(productAId, 100));
        }

        if (!await context.InventoryItems.AnyAsync(x => x.ProductId == productBId))
        {
            context.InventoryItems.Add(new InventoryItem(productBId, 0));
        }

        await context.SaveChangesAsync();
    }
}
