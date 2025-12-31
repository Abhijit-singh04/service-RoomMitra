using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RoomMitra.Domain.Entities;

namespace RoomMitra.Infrastructure.Persistence.Configurations;

internal sealed class FlatListingConfiguration : IEntityTypeConfiguration<FlatListing>
{
    public void Configure(EntityTypeBuilder<FlatListing> builder)
    {
        builder.ToTable("FlatListings");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Title)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Description)
            .HasMaxLength(4000)
            .IsRequired();

        builder.Property(x => x.City)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(x => x.Locality)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(x => x.Rent)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.Deposit)
            .HasColumnType("decimal(18,2)");

        builder.Property(x => x.PostedByUserId)
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .IsRequired();

        builder.Property(x => x.UpdatedAt);

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        builder.Property(x => x.Amenities)
            .HasColumnType("jsonb")
            .HasConversion(StringListJsonConverter(jsonOptions))
            .Metadata.SetValueComparer(StringListComparer());

        builder.Property(x => x.Preferences)
            .HasColumnType("jsonb")
            .HasConversion(StringListJsonConverter(jsonOptions))
            .Metadata.SetValueComparer(StringListComparer());

        builder.Property(x => x.Images)
            .HasColumnType("jsonb")
            .HasConversion(StringListJsonConverter(jsonOptions))
            .Metadata.SetValueComparer(StringListComparer());

        builder.HasIndex(x => new { x.City, x.Locality });
        builder.HasIndex(x => x.CreatedAt);
    }

    private static ValueConverter<List<string>, string> StringListJsonConverter(JsonSerializerOptions options)
    {
        return new ValueConverter<List<string>, string>(
            v => JsonSerializer.Serialize(v ?? new List<string>(), options),
            v => string.IsNullOrWhiteSpace(v) ? new List<string>() : JsonSerializer.Deserialize<List<string>>(v, options) ?? new List<string>()
        );
    }

    private static ValueComparer<List<string>> StringListComparer()
    {
        return new ValueComparer<List<string>>(
            (a, b) => (a ?? new List<string>()).SequenceEqual(b ?? new List<string>()),
            v => (v ?? new List<string>()).Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
            v => v.ToList()
        );
    }
}
