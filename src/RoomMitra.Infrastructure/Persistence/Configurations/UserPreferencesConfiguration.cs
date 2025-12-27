using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RoomMitra.Domain.Entities;
using RoomMitra.Infrastructure.Identity;

namespace RoomMitra.Infrastructure.Persistence.Configurations;

internal sealed class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        builder.ToTable("UserPreferences");

        builder.HasKey(up => up.Id);

        // Budget Preferences
        builder.Property(up => up.BudgetMin)
            .HasColumnType("decimal(18,2)");

        builder.Property(up => up.BudgetMax)
            .HasColumnType("decimal(18,2)");

        // Location Preferences
        builder.Property(up => up.PreferredCity)
            .HasMaxLength(100);

        var jsonOptions = new JsonSerializerOptions(JsonSerializerDefaults.Web);

        builder.Property(up => up.PreferredAreas)
            .HasConversion(
                v => JsonSerializer.Serialize(v, jsonOptions),
                v => JsonSerializer.Deserialize<List<string>>(v, jsonOptions) ?? new List<string>(),
                new ValueComparer<List<string>>(
                    (c1, c2) => c1!.SequenceEqual(c2!),
                    c => c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                    c => c.ToList()
                ))
            .HasColumnType("jsonb");

        // Preferences
        builder.Property(up => up.PreferredGender)
            .HasConversion<int?>();

        builder.Property(up => up.PreferredFood)
            .HasConversion<int?>();

        builder.Property(up => up.PreferredPropertyType)
            .HasConversion<int?>();

        builder.Property(up => up.PreferredFurnishing)
            .HasConversion<int?>();

        builder.Property(up => up.MoveInDate);

        // Audit Fields
        builder.Property(up => up.CreatedAt)
            .IsRequired();

        builder.Property(up => up.UpdatedAt);

        // Relationships
        builder.HasOne<AppUser>()
            .WithMany()
            .HasForeignKey(up => up.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        builder.HasIndex(up => up.UserId).IsUnique(); // One preference per user
        builder.HasIndex(up => up.PreferredCity);
        builder.HasIndex(up => new { up.BudgetMin, up.BudgetMax });
    }
}
