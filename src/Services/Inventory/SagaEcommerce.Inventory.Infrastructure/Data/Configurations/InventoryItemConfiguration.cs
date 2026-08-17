using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SagaEcommerce.Inventory.Domain.Entities;

namespace SagaEcommerce.Inventory.Infrastructure.Data.Configurations;

public class InventoryItemConfiguration : IEntityTypeConfiguration<InventoryItem>
{
    public void Configure(EntityTypeBuilder<InventoryItem> builder)
    {
        builder.ToTable("InventoryItems");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.ProductId)
            .IsRequired();

        builder.Property(x => x.AvailableQuantity)
            .IsRequired();

        // Optimistic concurrency control via PostgreSQL xmin system column
        builder.Property<uint>("xmin")
            .HasColumnType("xmin")
            .ValueGeneratedOnAddOrUpdate()
            .IsRowVersion();
    }
}
