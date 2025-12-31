using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomMitra.Domain.Entities;

namespace RoomMitra.Infrastructure.Persistence.Configurations;

public sealed class OtpRequestConfiguration : IEntityTypeConfiguration<OtpRequest>
{
    public void Configure(EntityTypeBuilder<OtpRequest> builder)
    {
        builder.ToTable("OtpRequests");

        builder.HasKey(o => o.Id);
        builder.HasIndex(o => o.RequestId).IsUnique();
        builder.HasIndex(o => o.PhoneNumber);

        builder.Property(o => o.PhoneNumber)
            .IsRequired()
            .HasMaxLength(32);

        builder.Property(o => o.RequestId)
            .IsRequired()
            .HasMaxLength(64);

        builder.Property(o => o.OtpHash)
            .IsRequired();

        builder.Property(o => o.Salt)
            .IsRequired();

        builder.Property(o => o.Channel)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(o => o.ExpiresAt)
            .IsRequired();

        builder.Property(o => o.LastSentAt)
            .IsRequired();
    }
}
