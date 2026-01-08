using RoomMitra.Domain.Entities;

namespace RoomMitra.Application.Abstractions.Chat;

/// <summary>
/// Repository interface for Conversation entity operations.
/// </summary>
public interface IConversationRepository
{
    /// <summary>
    /// Get a conversation by ID.
    /// </summary>
    Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// Get or create a conversation for the specified flat listing and interested user.
    /// </summary>
    Task<Conversation> GetOrCreateAsync(
        Guid flatListingId,
        Guid flatListingOwnerId,
        Guid interestedUserId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get all conversations for a user (either as owner or interested user).
    /// Includes related property and messages.
    /// </summary>
    Task<List<Conversation>> GetUserConversationsAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Get messages for a specific conversation.
    /// </summary>
    Task<List<Message>> GetConversationMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Add a new message to a conversation.
    /// </summary>
    Task<Message> AddMessageAsync(
        Guid conversationId,
        Guid senderId,
        string senderName,
        string content,
        CancellationToken cancellationToken);

    /// <summary>
    /// Mark messages as read for a specific user in a conversation.
    /// </summary>
    Task MarkMessagesAsReadAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Update the conversation's last message info.
    /// </summary>
    Task UpdateLastMessageAsync(
        Guid conversationId,
        string content,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get user display name by user ID.
    /// </summary>
    Task<string> GetUserDisplayNameAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Count unread messages in a conversation for a specific user.
    /// </summary>
    Task<int> CountUnreadMessagesAsync(Guid conversationId, Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Get flat listing owner ID and title by flat listing ID.
    /// </summary>
    Task<(Guid OwnerId, string Title)?> GetFlatListingInfoAsync(Guid flatListingId, CancellationToken cancellationToken);
}
