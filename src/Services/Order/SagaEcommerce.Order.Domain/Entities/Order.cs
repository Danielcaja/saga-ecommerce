using SagaEcommerce.Order.Domain.Enums;

namespace SagaEcommerce.Order.Domain.Entities;

public class Order
{
    public Guid Id { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public decimal Total { get; private set; }
    public OrderStatus Status { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Constructor required by EF Core
    protected Order() { }

    public Order(Guid productId, int quantity, decimal total)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("ProductId cannot be empty.", nameof(productId));

        if (quantity <= 0)
            throw new ArgumentException("Quantity must be greater than zero.", nameof(quantity));

        if (total <= 0)
            throw new ArgumentException("The order total value must be greater than zero.", nameof(total));

        Id = Guid.NewGuid();
        ProductId = productId;
        Quantity = quantity;
        Total = total;
        Status = OrderStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public void MarkAsApproved()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot approve an order with the current status: {Status}");

        Status = OrderStatus.Approved;
    }

    public void MarkAsRejected()
    {
        if (Status != OrderStatus.Pending)
            throw new InvalidOperationException($"Cannot reject an order with the current status: {Status}");

        Status = OrderStatus.Rejected;
    }
}
