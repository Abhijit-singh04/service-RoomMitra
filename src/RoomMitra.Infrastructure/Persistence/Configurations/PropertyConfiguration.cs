using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomMitra.Domain.Entities;
using RoomMitra.Infrastructure.Identity;

namespace RoomMitra.Infrastructure.Persistence.Configurations;

internal sealed class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> builder)
    {
        builder.ToTable("Properties");

        builder.HasKey(p => p.Id);

        // Basic Information
        builder.Property(p => p.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(p => p.PropertyType)
            .HasConversion<int>()
            .IsRequired();

        // Financial Details
        builder.Property(p => p.Rent)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(p => p.Deposit)
            .HasColumnType("decimal(18,2)")
            .IsRequired();

        builder.Property(p => p.AvailableFrom);

        // Location
        builder.Property(p => p.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(p => p.Area)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(p => p.Address)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(p => p.Latitude)
            .HasColumnType("decimal(10,7)");

        builder.Property(p => p.Longitude)
            .HasColumnType("decimal(10,7)");

        // Preferences
        builder.Property(p => p.PreferredGender)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(p => p.PreferredFood)
            .HasConversion<int>()
            .IsRequired();

        builder.Property(p => p.Furnishing)
            .HasConversion<int>()
            .IsRequired();

        // Status
        builder.Property(p => p.Status)
            .HasConversion<int>()
            .IsRequired();

        // Audit Fields
        builder.Property(p => p.CreatedAt)
            .IsRequired();

        builder.Property(p => p.UpdatedAt);

        // Relationships
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(p => p.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.Images)
            .WithOne(i => i.Property)
            .HasForeignKey(i => i.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(p => p.PropertyAmenities)
            .WithOne(pa => pa.Property)
            .HasForeignKey(pa => pa.PropertyId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for search and filtering
        builder.HasIndex(p => p.UserId);
        builder.HasIndex(p => p.City);
        builder.HasIndex(p => p.Rent);
        builder.HasIndex(p => p.PropertyType);
        builder.HasIndex(p => p.Status);
        builder.HasIndex(p => p.AvailableFrom);
        builder.HasIndex(p => new { p.City, p.Rent, p.Status }); // Composite index for common queries
    }
}
