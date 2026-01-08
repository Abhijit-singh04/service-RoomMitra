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
        builder.Property(c => c.PropertyOwnerId)
            .IsRequired();

        builder.Property(c => c.InterestedUserId)
            .IsRequired();

        builder.Property(c => c.PropertyId)
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
        builder.HasOne(c => c.Property)
            .WithMany()
            .HasForeignKey(c => c.PropertyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(c => c.Messages)
            .WithOne(m => m.Conversation)
            .HasForeignKey(m => m.ConversationId)
            .OnDelete(DeleteBehavior.Cascade);

        // Unique constraint: one conversation per (PropertyOwnerId, InterestedUserId, PropertyId) tuple
        builder.HasIndex(c => new { c.PropertyOwnerId, c.InterestedUserId, c.PropertyId })
            .IsUnique()
            .HasDatabaseName("IX_Conversations_Unique_Participants_Property");

        // Indexes for querying conversations by user
        builder.HasIndex(c => c.PropertyOwnerId)
            .HasDatabaseName("IX_Conversations_PropertyOwnerId");

        builder.HasIndex(c => c.InterestedUserId)
            .HasDatabaseName("IX_Conversations_InterestedUserId");

        builder.HasIndex(c => c.PropertyId)
            .HasDatabaseName("IX_Conversations_PropertyId");

        builder.HasIndex(c => c.LastMessageAt)
            .HasDatabaseName("IX_Conversations_LastMessageAt");
    }
}
