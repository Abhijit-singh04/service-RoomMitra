namespace RoomMitra.Application.Models.Chat;

/// <summary>
/// DTO for a single message in a conversation.
/// </summary>
public sealed record MessageDto(
    Guid Id,
    Guid ConversationId,
    Guid SenderId,
    string SenderName,
    string Content,
    bool IsRead,
    DateTimeOffset CreatedAt
);
