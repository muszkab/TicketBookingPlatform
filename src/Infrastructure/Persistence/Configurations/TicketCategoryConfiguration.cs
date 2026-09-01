using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Events;

namespace Infrastructure.Persistence.Configurations;

public class TicketCategoryConfiguration : IEntityTypeConfiguration<TicketCategory>
{
    public void Configure(EntityTypeBuilder<TicketCategory> builder)
    {
        builder.ToTable("TicketCategories");

        builder.HasKey(tc => tc.Id);

        builder.Property(tc => tc.EventId).IsRequired();

        builder.Property(tc => tc.Name)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(tc => tc.TotalQuantity).IsRequired();
        builder.Property(tc => tc.AvailableQuantity).IsRequired();

        builder.OwnsOne(tc => tc.Price, price =>
        {
            price.Property(p => p.Amount)
                .HasColumnName("PriceAmount")
                .HasColumnType("decimal(18,2)")
                .IsRequired();

            price.Property(p => p.Currency)
                .HasColumnName("PriceCurrency")
                .HasMaxLength(3)
                .IsRequired();
        });

        builder.HasIndex(tc => tc.EventId);
    }
}
