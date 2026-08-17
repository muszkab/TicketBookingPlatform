using Domain.Orders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.OrderId).IsRequired();
        builder.Property(i => i.TicketCategoryId).IsRequired();
        builder.Property(i => i.Quantity).IsRequired();

        builder.OwnsOne(i => i.UnitPrice, price =>
        {
            price.Property(p => p.Amount)
                .HasColumnName("UnitPriceAmount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            price.Property(p => p.Currency)
                .HasColumnName("UnitPriceCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.Ignore(i => i.LineTotal);

        builder.HasIndex(i => i.OrderId);
        builder.HasIndex(i => i.TicketCategoryId);

        builder.HasOne(i => i.TicketCategory)
            .WithMany()
            .HasForeignKey(i => i.TicketCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
