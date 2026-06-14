using SagaEcommerce.Order.Application.DTOs;

namespace SagaEcommerce.Order.Application.Interfaces;

public interface IOrderAppService
{
    Task<OrderDto> CreateOrderAsync(CreateOrderDto createOrderDto);
    Task<OrderDto?> GetByIdAsync(Guid id);
}
