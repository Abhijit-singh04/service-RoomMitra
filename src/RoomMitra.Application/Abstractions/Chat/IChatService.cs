using RoomMitra.Application.Models.Chat;

namespace RoomMitra.Application.Abstractions.Chat;

/// <summary>
/// Service interface for chat operations.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Get all conversations for the current authenticated user.
    /// </summary>
    Task<List<ConversationDto>> GetMyConversationsAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Get messages for a specific conversation.
    /// Validates that the current user is a participant.
    /// Marks unread messages from the other user as read.
    /// </summary>
    Task<List<MessageDto>> GetConversationMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Get or create a conversation for a flat listing.
    /// Current user is the interested user, flat listing owner is determined from flat listing.
    /// </summary>
    Task<ConversationDto> GetOrCreateConversationAsync(
        Guid flatListingId,
        CancellationToken cancellationToken);

    /// <summary>
    /// Send a message in a conversation.
    /// Validates that the current user is a participant.
    /// Returns the created message DTO.
    /// </summary>
    Task<MessageDto> SendMessageAsync(
        Guid conversationId,
        string content,
        CancellationToken cancellationToken);

    /// <summary>
    /// Validate that the current user is a participant in the conversation.
    /// Throws UnauthorizedAccessException if not.
    /// </summary>
    Task ValidateParticipantAsync(Guid conversationId, CancellationToken cancellationToken);
}
