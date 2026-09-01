using Domain.Tickets;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class TicketConfiguration : IEntityTypeConfiguration<Ticket>
{
    public void Configure(EntityTypeBuilder<Ticket> builder)
    {
        builder.ToTable("Tickets");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.OrderId).IsRequired();
        builder.Property(t => t.OrderItemId).IsRequired();
        builder.Property(t => t.EventId).IsRequired();
        builder.Property(t => t.TicketCategoryId).IsRequired();

        builder.Property(t => t.Code)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(t => t.Status)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(t => t.CreatedAt).IsRequired();
        builder.Property(t => t.UsedAt);

        builder.HasIndex(t => t.Code).IsUnique();
        builder.HasIndex(t => t.OrderId);
        builder.HasIndex(t => t.EventId);

        builder.HasOne(t => t.Order)
            .WithMany()
            .HasForeignKey(t => t.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.Event)
            .WithMany()
            .HasForeignKey(t => t.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(t => t.TicketCategory)
            .WithMany()
            .HasForeignKey(t => t.TicketCategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
