using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomMitra.Domain.Entities;

namespace RoomMitra.Infrastructure.Persistence.Configurations;

internal sealed class AmenityConfiguration : IEntityTypeConfiguration<Amenity>
{
    public void Configure(EntityTypeBuilder<Amenity> builder)
    {
        builder.ToTable("Amenities");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Name)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(a => a.Description)
            .HasMaxLength(500);

        builder.Property(a => a.Icon)
            .HasMaxLength(100);

        // Relationships
        builder.HasMany(a => a.PropertyAmenities)
            .WithOne(pa => pa.Amenity)
            .HasForeignKey(pa => pa.AmenityId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(a => a.Name).IsUnique();

        // Seed common amenities
        builder.HasData(
            new Amenity { Id = Guid.Parse("11111111-1111-1111-1111-111111111111"), Name = "WiFi", Description = "High-speed internet connection", Icon = "wifi" },
            new Amenity { Id = Guid.Parse("22222222-2222-2222-2222-222222222222"), Name = "Air Conditioning", Description = "AC in rooms", Icon = "ac_unit" },
            new Amenity { Id = Guid.Parse("33333333-3333-3333-3333-333333333333"), Name = "Washing Machine", Description = "Washing machine available", Icon = "local_laundry_service" },
            new Amenity { Id = Guid.Parse("44444444-4444-4444-4444-444444444444"), Name = "Parking", Description = "Vehicle parking space", Icon = "local_parking" },
            new Amenity { Id = Guid.Parse("55555555-5555-5555-5555-555555555555"), Name = "Gym", Description = "Fitness center", Icon = "fitness_center" },
            new Amenity { Id = Guid.Parse("66666666-6666-6666-6666-666666666666"), Name = "Power Backup", Description = "Generator backup", Icon = "power" },
            new Amenity { Id = Guid.Parse("77777777-7777-7777-7777-777777777777"), Name = "Water Purifier", Description = "RO water system", Icon = "water_drop" },
            new Amenity { Id = Guid.Parse("88888888-8888-8888-8888-888888888888"), Name = "Refrigerator", Description = "Fridge available", Icon = "kitchen" },
            new Amenity { Id = Guid.Parse("99999999-9999-9999-9999-999999999999"), Name = "TV", Description = "Television", Icon = "tv" },
            new Amenity { Id = Guid.Parse("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"), Name = "Security", Description = "24/7 security", Icon = "security" }
        );
    }
}
