namespace SagaEcommerce.Order.Application.DTOs;

public record OrderDto(Guid Id, Guid ClientId, decimal Total, string Status, DateTime CreatedAt);
