using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Domain.Events;

namespace Infrastructure.Persistence.Configurations;

public class EventConfiguration : IEntityTypeConfiguration<Event>
{
    public void Configure(EntityTypeBuilder<Event> builder)
    {
        builder.ToTable("Events");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(e => e.Category)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.Status)
            .HasConversion<string>()
            .HasMaxLength(50);

        builder.Property(e => e.StartsAt).IsRequired();
        builder.Property(e => e.EndsAt).IsRequired();
        builder.Property(e => e.LocationId).IsRequired();

        builder.Property(e => e.Description).HasMaxLength(2000);

        builder.HasIndex(e => e.LocationId);
        builder.HasIndex(e => e.StartsAt);

        builder.HasMany(e => e.TicketCategories)
            .WithOne()
            .HasForeignKey(tc => tc.EventId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Metadata
            .FindNavigation(nameof(Event.TicketCategories))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
