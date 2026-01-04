using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomMitra.Infrastructure.Identity;

namespace RoomMitra.Infrastructure.Persistence.Configurations;

internal sealed class AppUserConfiguration : IEntityTypeConfiguration<AppUser>
{
    public void Configure(EntityTypeBuilder<AppUser> builder)
    {
        builder.ToTable("AspNetUsers");

        builder.Property(u => u.Name)
            .HasMaxLength(200)
            .IsRequired(false); // Name can be empty for new phone users until profile completion

        builder.Property(u => u.ProfileImageUrl)
            .HasMaxLength(500);

        builder.Property(u => u.Occupation)
            .HasMaxLength(100);

        builder.Property(u => u.Bio)
            .HasMaxLength(1000);

        builder.Property(u => u.Gender)
            .HasConversion<int>();

        builder.Property(u => u.IsVerified)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(u => u.CreatedAt)
            .IsRequired();

        builder.Property(u => u.UpdatedAt);

        // New fields for auth flow
        builder.Property(u => u.AuthProvider)
            .HasMaxLength(50)
            .IsRequired()
            .HasDefaultValue("email");

        builder.Property(u => u.ExternalId)
            .HasMaxLength(255);

        builder.Property(u => u.IsProfileComplete)
            .IsRequired()
            .HasDefaultValue(false);

        // Indexes for common queries
        builder.HasIndex(u => u.Email).IsUnique().HasFilter("\"Email\" IS NOT NULL");
        builder.HasIndex(u => u.PhoneNumber);
        builder.HasIndex(u => u.IsVerified);
        builder.HasIndex(u => u.ExternalId).IsUnique().HasFilter("\"ExternalId\" IS NOT NULL");
        builder.HasIndex(u => u.AuthProvider);
    }
}
