namespace RoomMitra.Application.Models.Chat;

/// <summary>
/// Request to create or get a conversation for a property.
/// </summary>
public sealed record GetOrCreateConversationRequest(
    Guid PropertyId
);
