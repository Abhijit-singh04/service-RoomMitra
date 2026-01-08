namespace RoomMitra.Application.Models.Chat;

/// <summary>
/// Request to create or get a conversation for a flat listing.
/// </summary>
public sealed record GetOrCreateConversationRequest(
    Guid FlatListingId
);
