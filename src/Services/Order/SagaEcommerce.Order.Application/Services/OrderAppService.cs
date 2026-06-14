using FluentValidation;
using SagaEcommerce.Order.Application.DTOs;
using SagaEcommerce.Order.Application.Events;
using SagaEcommerce.Order.Application.Interfaces;
using SagaEcommerce.Order.Domain.Repositories;
using OrderEntity = SagaEcommerce.Order.Domain.Entities.Order;

namespace SagaEcommerce.Order.Application.Services;

public class OrderAppService(
    IOrderRepository orderRepository,
    IOrderEventPublisher eventPublisher,
    IValidator<CreateOrderDto> validator) : IOrderAppService
{
    public async Task<OrderDto> CreateOrderAsync(CreateOrderDto createOrderDto)
    {
        // 1. Validation with FluentValidation
        var validationResult = await validator.ValidateAsync(createOrderDto);
        if (!validationResult.IsValid)
        {
            throw new ValidationException(validationResult.Errors);
        }

        // 2. Domain Entity Creation (DDD)
        var order = new OrderEntity(createOrderDto.ClientId, createOrderDto.Total);

        // 3. Save to Database
        await orderRepository.AddAsync(order);
        await orderRepository.SaveChangesAsync();

        // 4. Publish Integration Event on RabbitMQ (using routing key "order.created")
        var @event = new OrderCreatedEvent(order.Id, order.ClientId, order.Total, order.CreatedAt);
        await eventPublisher.PublishAsync(@event, "order.created");

        // 5. Return mapped DTO
        return new OrderDto(
            order.Id,
            order.ClientId,
            order.Total,
            order.Status.ToString(),
            order.CreatedAt
        );
    }

    public async Task<OrderDto?> GetByIdAsync(Guid id)
    {
        var order = await orderRepository.GetByIdAsync(id);
        OrderDto? result = null;

        if (order != null)
        {
            result = new OrderDto(
                order.Id,
                order.ClientId,
                order.Total,
                order.Status.ToString(),
                order.CreatedAt
            );
        }

        return result;
    }
}
