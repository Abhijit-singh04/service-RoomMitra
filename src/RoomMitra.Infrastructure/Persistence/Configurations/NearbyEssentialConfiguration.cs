using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomMitra.Domain.Entities;

namespace RoomMitra.Infrastructure.Persistence.Configurations;

/// <summary>
/// EF configuration for NearbyEssential entity.
/// </summary>
internal sealed class NearbyEssentialConfiguration : IEntityTypeConfiguration<NearbyEssential>
{
    public void Configure(EntityTypeBuilder<NearbyEssential> builder)
    {
        builder.ToTable("nearby_essentials");
        
        builder.HasKey(x => x.Id);
        
        builder.Property(x => x.Id)
            .HasColumnName("id");
        
        builder.Property(x => x.FlatListingId)
            .HasColumnName("flat_listing_id")
            .IsRequired();
        
        builder.Property(x => x.Category)
            .HasColumnName("category")
            .HasMaxLength(50)
            .IsRequired();
        
        builder.Property(x => x.Name)
            .HasColumnName("name")
            .HasMaxLength(200)
            .IsRequired();
        
        builder.Property(x => x.DistanceMeters)
            .HasColumnName("distance_meters")
            .IsRequired();
        
        builder.Property(x => x.FetchedAt)
            .HasColumnName("fetched_at")
            .IsRequired();
        
        // Index for efficient lookups by listing
        builder.HasIndex(x => x.FlatListingId);
    }
}
