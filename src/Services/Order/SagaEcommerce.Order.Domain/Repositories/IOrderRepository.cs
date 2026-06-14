namespace SagaEcommerce.Order.Domain.Repositories;

public interface IOrderRepository
{
    Task<Entities.Order?> GetByIdAsync(Guid id);
    Task AddAsync(Entities.Order order);
    Task UpdateAsync(Entities.Order order);
    Task<bool> SaveChangesAsync();
}
