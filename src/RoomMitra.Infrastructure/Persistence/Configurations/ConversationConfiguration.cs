using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RoomMitra.Domain.Entities;

namespace RoomMitra.Infrastructure.Persistence.Configurations;

internal sealed class ConversationConfiguration : IEntityTypeConfiguration<Conversation>
{
    public void Configure(EntityTypeBuilder<Conversation> builder)
    {
        builder.ToTable("Conversations");

        builder.HasKey(c => c.Id);

        // Foreign Keys
        builder.Property(c => c.FlatListingOwnerId)
            .IsRequired();

        builder.Property(c => c.InterestedUserId)
            .IsRequired();

        builder.Property(c => c.FlatListingId)
            .IsRequired();

        // Last Message Info
        builder.Property(c => c.LastMessageContent)
            .HasMaxLength(500);

        builder.Property(c => c.LastMessageAt);

        // Audit fields
        builder.Property(c => c.CreatedAt)
            .IsRequired();

        builder.Property(c => c.UpdatedAt);

        // Relationships
        builder.HasOne(c => c.FlatListing)
            .WithMany()
            .HasForeignKey(c => c.FlatListingId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one conversation per (FlatListingOwnerId, InterestedUserId, FlatListingId) tuple
        builder.HasIndex(c => new { c.FlatListingOwnerId, c.InterestedUserId, c.FlatListingId })
            .IsUnique()
            .HasDatabaseName("IX_Conversations_Unique_Participants_FlatListing");

        // Indexes for querying conversations by user
        builder.HasIndex(c => c.FlatListingOwnerId)
            .HasDatabaseName("IX_Conversations_FlatListingOwnerId");

        builder.HasIndex(c => c.InterestedUserId)
            .HasDatabaseName("IX_Conversations_InterestedUserId");

        builder.HasIndex(c => c.FlatListingId)
            .HasDatabaseName("IX_Conversations_FlatListingId");

        builder.HasIndex(c => c.LastMessageAt)
            .HasDatabaseName("IX_Conversations_LastMessageAt");
    }
}
