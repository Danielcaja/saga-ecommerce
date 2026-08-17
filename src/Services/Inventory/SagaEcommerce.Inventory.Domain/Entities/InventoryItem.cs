namespace SagaEcommerce.Inventory.Domain.Entities;

public class InventoryItem
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public int AvailableQuantity { get; private set; }

    // Constructor for EF Core
    protected InventoryItem() { }

    public InventoryItem(Guid productId, int availableQuantity)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty.", nameof(productId));

        if (availableQuantity < 0)
            throw new ArgumentException("Available quantity cannot be negative.", nameof(availableQuantity));

        Id = Guid.NewGuid();
        ProductId = productId;
        AvailableQuantity = availableQuantity;
    }

    public bool IsQuantityAvailable(int quantity)
    {
        return AvailableQuantity >= quantity;
    }

    public void Reserve(int quantity)
    {
        if (quantity <= 0)
            throw new ArgumentException("Quantity to reserve must be greater than zero.", nameof(quantity));

        if (!IsQuantityAvailable(quantity))
            throw new InvalidOperationException($"Insufficient stock. Available: {AvailableQuantity}, Requested: {quantity}");

        AvailableQuantity -= quantity;
    }
}
