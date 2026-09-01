using Domain.Locations;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Persistence.Configurations;

public class LocationConfiguration : IEntityTypeConfiguration<Location>
{
    public void Configure(EntityTypeBuilder<Location> builder)
    {
        builder.ToTable("Locations");

        builder.HasKey(l => l.Id);

        builder.Property(l => l.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(l => l.City)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.Country)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(l => l.Street).HasMaxLength(200);
        builder.Property(l => l.PostalCode).HasMaxLength(20);

        builder.Property(l => l.Capacity).IsRequired();

        builder.HasIndex(l => new { l.Name, l.City, l.Country }).IsUnique();
    }
}
