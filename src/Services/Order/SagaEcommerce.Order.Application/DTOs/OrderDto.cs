namespace SagaEcommerce.Order.Application.DTOs;

public record OrderDto(Guid Id, Guid ProductId, int Quantity, decimal Total, string Status, DateTime CreatedAt);
