using Microsoft.AspNetCore.Mvc;
using SagaEcommerce.Inventory.Application.DTOs;
using SagaEcommerce.Inventory.Application.Interfaces;

namespace SagaEcommerce.Inventory.API.Controllers;

[ApiController]
[Route("api/inventory")]
public class InventoryController(IInventoryAppService inventoryAppService, ILogger<InventoryController> logger) : ControllerBase
{
    [HttpGet("{productId:guid}")]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(InventoryItemDto))]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByProductId(Guid productId)
    {
        logger.LogInformation("Checking inventory for Product: {ProductId}", productId);
        var itemDto = await inventoryAppService.GetByProductIdAsync(productId);

        if (itemDto == null)
        {
            logger.LogWarning("Inventory item not found for Product: {ProductId}", productId);
            return NotFound(new { Message = $"Inventory item not found for product {productId}" });
        }

        return Ok(itemDto);
    }

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK, Type = typeof(IEnumerable<InventoryItemDto>))]
    public async Task<IActionResult> GetAll()
    {
        logger.LogInformation("Received request to get all inventory items.");
        var items = await inventoryAppService.GetAllAsync();
        return Ok(items);
    }
}
