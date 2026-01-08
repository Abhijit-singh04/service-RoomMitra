namespace RoomMitra.Application.Models.Chat;

/// <summary>
/// DTO for a conversation in the conversation list.
/// </summary>
public sealed record ConversationDto(
    Guid Id,
    Guid FlatListingId,
    string FlatListingTitle,
    Guid OtherUserId,
    string OtherUserName,
    string? LastMessageContent,
    DateTimeOffset? LastMessageAt,
    int UnreadCount,
    DateTimeOffset CreatedAt
);
