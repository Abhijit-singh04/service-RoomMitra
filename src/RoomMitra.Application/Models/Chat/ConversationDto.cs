namespace RoomMitra.Application.Models.Chat;

/// <summary>
/// DTO for a conversation in the conversation list.
/// </summary>
public sealed record ConversationDto(
    Guid Id,
    Guid PropertyId,
    string PropertyTitle,
    Guid OtherUserId,
    string OtherUserName,
    string? LastMessageContent,
    DateTimeOffset? LastMessageAt,
    int UnreadCount,
    DateTimeOffset CreatedAt
);
