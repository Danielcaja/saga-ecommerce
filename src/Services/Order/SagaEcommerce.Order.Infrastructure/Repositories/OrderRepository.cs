using Microsoft.EntityFrameworkCore;
using SagaEcommerce.Order.Domain.Repositories;
using SagaEcommerce.Order.Infrastructure.Data;
using OrderEntity = SagaEcommerce.Order.Domain.Entities.Order;

namespace SagaEcommerce.Order.Infrastructure.Repositories;

public class OrderRepository(OrderDbContext context) : IOrderRepository
{
    public async Task<OrderEntity?> GetByIdAsync(Guid id)
    {
        return await context.Orders.FindAsync(id);
    }

    public async Task<IEnumerable<OrderEntity>> GetAllAsync()
    {
        return await context.Orders.AsNoTracking().ToListAsync();
    }

    public async Task AddAsync(OrderEntity order)
    {
        await context.Orders.AddAsync(order);
    }

    public async Task UpdateAsync(OrderEntity order)
    {
        context.Orders.Update(order);
        await Task.CompletedTask;
    }

    public async Task<bool> SaveChangesAsync()
    {
        return await context.SaveChangesAsync() > 0;
    }
}
