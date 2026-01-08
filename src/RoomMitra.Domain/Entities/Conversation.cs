using RoomMitra.Domain.Common;

namespace RoomMitra.Domain.Entities;

/// <summary>
/// Represents a 1-to-1 chat conversation between a property owner and an interested user for a specific property.
/// Each (PropertyOwnerId, InterestedUserId, PropertyId) tuple maps to exactly one conversation.
/// </summary>
public sealed class Conversation : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The ID of the user who owns the property (matches Property.UserId).
    /// Foreign key to AspNetUsers.Id.
    /// </summary>
    public Guid PropertyOwnerId { get; set; }

    /// <summary>
    /// The ID of the user who is interested in the property.
    /// Foreign key to AspNetUsers.Id.
    /// </summary>
    public Guid InterestedUserId { get; set; }

    /// <summary>
    /// The ID of the property this conversation is about.
    /// Foreign key to Properties.Id.
    /// </summary>
    public Guid PropertyId { get; set; }

    /// <summary>
    /// The last message content (for quick preview in conversation list).
    /// </summary>
    public string? LastMessageContent { get; set; }

    /// <summary>
    /// When the last message was sent (for sorting conversations).
    /// </summary>
    public DateTimeOffset? LastMessageAt { get; set; }

    // Navigation Properties
    public Property Property { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
