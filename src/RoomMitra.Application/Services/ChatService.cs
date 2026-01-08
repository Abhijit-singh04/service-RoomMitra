using RoomMitra.Application.Abstractions.Chat;
using RoomMitra.Application.Abstractions.Repositories;
using RoomMitra.Application.Abstractions.Security;
using RoomMitra.Application.Models.Chat;

namespace RoomMitra.Application.Services;

public sealed class ChatService : IChatService
{
    private readonly IConversationRepository _conversationRepository;
    private readonly IUserContext _userContext;

    public ChatService(
        IConversationRepository conversationRepository,
        IUserContext userContext)
    {
        _conversationRepository = conversationRepository;
        _userContext = userContext;
    }

    public async Task<List<ConversationDto>> GetMyConversationsAsync(CancellationToken cancellationToken)
    {
        var userId = _userContext.UserId ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var conversations = await _conversationRepository.GetUserConversationsAsync(userId, cancellationToken);

        var dtos = new List<ConversationDto>();

        foreach (var conversation in conversations)
        {
            // Determine who the "other" user is
            var isOwner = conversation.PropertyOwnerId == userId;
            var otherUserId = isOwner ? conversation.InterestedUserId : conversation.PropertyOwnerId;

            // Get other user's name
            var otherUserName = await _conversationRepository.GetUserDisplayNameAsync(otherUserId, cancellationToken);

            // Count unread messages (messages from the other user)
            var unreadCount = await _conversationRepository.CountUnreadMessagesAsync(
                conversation.Id,
                userId,
                cancellationToken);

            var dto = new ConversationDto(
                conversation.Id,
                conversation.PropertyId,
                conversation.Property.Title,
                otherUserId,
                otherUserName,
                conversation.LastMessageContent,
                conversation.LastMessageAt,
                unreadCount,
                conversation.CreatedAt
            );

            dtos.Add(dto);
        }

        return dtos;
    }

    public async Task<List<MessageDto>> GetConversationMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        var userId = _userContext.UserId ?? throw new UnauthorizedAccessException("User is not authenticated.");

        // Validate participant
        await ValidateParticipantAsync(conversationId, cancellationToken);

        // Mark messages from the other user as read
        await _conversationRepository.MarkMessagesAsReadAsync(conversationId, userId, cancellationToken);

        // Get messages
        var messages = await _conversationRepository.GetConversationMessagesAsync(conversationId, cancellationToken);

        return messages.Select(m => new MessageDto(
            m.Id,
            m.ConversationId,
            m.SenderId,
            m.SenderName,
            m.Content,
            m.IsRead,
            m.CreatedAt
        )).ToList();
    }

    public async Task<ConversationDto> GetOrCreateConversationAsync(
        Guid propertyId,
        CancellationToken cancellationToken)
    {
        var userId = _userContext.UserId ?? throw new UnauthorizedAccessException("User is not authenticated.");

        // Get property info to determine owner
        var propertyInfo = await _conversationRepository.GetPropertyInfoAsync(propertyId, cancellationToken);

        if (propertyInfo == null)
        {
            throw new InvalidOperationException($"Property with ID {propertyId} not found.");
        }

        var (propertyOwnerId, propertyTitle) = propertyInfo.Value;

        // Current user is the interested user
        var conversation = await _conversationRepository.GetOrCreateAsync(
            propertyId,
            propertyOwnerId,
            userId,
            cancellationToken);

        // Get owner's name
        var ownerName = await _conversationRepository.GetUserDisplayNameAsync(propertyOwnerId, cancellationToken);

        // Count unread messages from owner
        var unreadCount = await _conversationRepository.CountUnreadMessagesAsync(
            conversation.Id,
            userId,
            cancellationToken);

        return new ConversationDto(
            conversation.Id,
            propertyId,
            propertyTitle,
            propertyOwnerId,
            ownerName,
            conversation.LastMessageContent,
            conversation.LastMessageAt,
            unreadCount,
            conversation.CreatedAt
        );
    }

    public async Task<MessageDto> SendMessageAsync(
        Guid conversationId,
        string content,
        CancellationToken cancellationToken)
    {
        var userId = _userContext.UserId ?? throw new UnauthorizedAccessException("User is not authenticated.");

        // Validate participant
        await ValidateParticipantAsync(conversationId, cancellationToken);

        // Get sender's name
        var senderName = await _conversationRepository.GetUserDisplayNameAsync(userId, cancellationToken);

        // Add message
        var message = await _conversationRepository.AddMessageAsync(
            conversationId,
            userId,
            senderName,
            content,
            cancellationToken);

        // Update conversation's last message
        await _conversationRepository.UpdateLastMessageAsync(
            conversationId,
            content,
            message.CreatedAt,
            cancellationToken);

        return new MessageDto(
            message.Id,
            message.ConversationId,
            message.SenderId,
            message.SenderName,
            message.Content,
            message.IsRead,
            message.CreatedAt
        );
    }

    public async Task ValidateParticipantAsync(Guid conversationId, CancellationToken cancellationToken)
    {
        var userId = _userContext.UserId ?? throw new UnauthorizedAccessException("User is not authenticated.");

        var conversation = await _conversationRepository.GetByIdAsync(conversationId, cancellationToken);

        if (conversation == null)
        {
            throw new InvalidOperationException($"Conversation with ID {conversationId} not found.");
        }

        if (conversation.PropertyOwnerId != userId && conversation.InterestedUserId != userId)
        {
            throw new UnauthorizedAccessException("You are not a participant in this conversation.");
        }
    }
}
