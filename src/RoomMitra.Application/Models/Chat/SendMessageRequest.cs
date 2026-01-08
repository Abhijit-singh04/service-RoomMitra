namespace RoomMitra.Application.Models.Chat;

/// <summary>
/// Request to send a message in a conversation.
/// </summary>
public sealed record SendMessageRequest(
    Guid ConversationId,
    string Content
);
