namespace SagaEcommerce.Order.Application.DTOs;

public record CreateOrderDto(Guid ProductId, int Quantity, decimal Total);
