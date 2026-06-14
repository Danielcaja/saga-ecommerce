namespace SagaEcommerce.Order.Application.DTOs;

public record CreateOrderDto(Guid ClientId, decimal Total);
