using RoomMitra.Domain.Common;

namespace RoomMitra.Domain.Entities;

/// <summary>
/// Represents a single message in a conversation.
/// </summary>
public sealed class Message : AuditableEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// The conversation this message belongs to.
    /// Foreign key to Conversations.Id.
    /// </summary>
    public Guid ConversationId { get; set; }

    /// <summary>
    /// The user who sent this message.
    /// Foreign key to AspNetUsers.Id.
    /// </summary>
    public Guid SenderId { get; set; }

    /// <summary>
    /// The name of the sender (denormalized for quick display).
    /// </summary>
    public string SenderName { get; set; } = string.Empty;

    /// <summary>
    /// The message content.
    /// </summary>
    public string Content { get; set; } = string.Empty;

    /// <summary>
    /// Whether the message has been read by the recipient.
    /// </summary>
    public bool IsRead { get; set; } = false;

    // Navigation Properties
    public Conversation Conversation { get; set; } = null!;
}
