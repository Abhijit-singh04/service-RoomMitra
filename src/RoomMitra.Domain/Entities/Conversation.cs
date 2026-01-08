using RoomMitra.Domain.Common;

namespace RoomMitra.Domain.Entities;

/// <summary>
/// Represents a 1-to-1 chat conversation between a flat listing owner and an interested user for a specific flat listing.
/// Each (FlatListingOwnerId, InterestedUserId, FlatListingId) tuple maps to exactly one conversation.
/// </summary>
public sealed class Conversation : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The ID of the user who owns the flat listing (matches FlatListing.PostedByUserId).
    /// Foreign key to AspNetUsers.Id.
    /// </summary>
    public Guid FlatListingOwnerId { get; set; }

    /// <summary>
    /// The ID of the user who is interested in the flat listing.
    /// Foreign key to AspNetUsers.Id.
    /// </summary>
    public Guid InterestedUserId { get; set; }

    /// <summary>
    /// The ID of the flat listing this conversation is about.
    /// Foreign key to FlatListings.Id.
    /// </summary>
    public Guid FlatListingId { get; set; }

    /// <summary>
    /// The last message content (for quick preview in conversation list).
    /// </summary>
    public string? LastMessageContent { get; set; }

    /// <summary>
    /// When the last message was sent (for sorting conversations).
    /// </summary>
    public DateTimeOffset? LastMessageAt { get; set; }

    // Navigation Properties
    public FlatListing FlatListing { get; set; } = null!;
    public ICollection<Message> Messages { get; set; } = new List<Message>();
}
