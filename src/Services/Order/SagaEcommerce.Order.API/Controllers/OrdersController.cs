using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using SagaEcommerce.Order.Application.DTOs;
using SagaEcommerce.Order.Application.Interfaces;

namespace SagaEcommerce.Order.API.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController(IOrderAppService orderAppService, ILogger<OrdersController> logger) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status201Created, Type = typeof(OrderDto))]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderDto createOrderDto)
    {
        IActionResult result;
        try
        {
            logger.LogInformation("Received request to create order. ProductId: {ProductId}, Quantity: {Quantity}, Total: {Total}", createOrderDto.ProductId, createOrderDto.Quantity, createOrderDto.Total);
            
            var orderDto = await orderAppService.CreateOrderAsync(createOrderDto);
            
            result = CreatedAtAction(nameof(GetById), new { id = orderDto.Id }, orderDto);
        }
        catch (ValidationException ex)
        {
            logger.LogWarning(ex, "Validation error occurred while creating order.");
            
            var errors = ex.Errors.Select(e => new { Field = e.PropertyName, Error = e.ErrorMessage });
            result = BadRequest(new { Message = "Validation error on input data.", Errors = errors });
        }
        catch (ArgumentException ex)
        {
            logger.LogWarning(ex, "Domain argument validation error.");
            result = BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Unexpected error occurred while creating order.");
            result = StatusCode(500, new { Message = "Internal server error occurred while processing the order." });
        }

        return result;
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(OrderDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(Guid id)
    {
        var orderDto = await orderAppService.GetByIdAsync(id);
        IActionResult result;

        if (orderDto == null)
        {
            result = NotFound(new { Message = $"Order with ID {id} not found." });
        }
        else
        {
            result = Ok(orderDto);
        }

        return result;
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<OrderDto>))]
    public async Task<IActionResult> GetAll()
    {
        logger.LogInformation("Received request to get all orders.");
        var orders = await orderAppService.GetAllAsync();
        return Ok(orders);
    }
}
