using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using RoomMitra.Application.Abstractions.Chat;
using RoomMitra.Application.Abstractions.Security;
using System.Security.Claims;

namespace RoomMitra.Api.Hubs;

/// <summary>
/// SignalR Hub for real-time chat functionality.
/// Handles joining conversation groups and broadcasting messages.
/// </summary>
[Authorize]
public sealed class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly IUserContext _userContext;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(
        IChatService chatService,
        IUserContext userContext,
        ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _userContext = userContext;
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects to the hub.
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var userId = _userContext.UserId;
        _logger.LogInformation("User {UserId} connected to ChatHub with ConnectionId {ConnectionId}",
            userId, Context.ConnectionId);

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects from the hub.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = _userContext.UserId;
        _logger.LogInformation("User {UserId} disconnected from ChatHub with ConnectionId {ConnectionId}",
            userId, Context.ConnectionId);

        if (exception != null)
        {
            _logger.LogError(exception, "User {UserId} disconnected with error", userId);
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Join a conversation group to receive real-time messages.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation to join.</param>
    public async Task JoinConversation(Guid conversationId)
    {
        try
        {
            var userId = _userContext.UserId;
            if (userId == null)
            {
                _logger.LogWarning("Unauthenticated user attempted to join conversation {ConversationId}",
                    conversationId);
                throw new HubException("User is not authenticated.");
            }

            // Validate that the user is a participant in this conversation
            await _chatService.ValidateParticipantAsync(conversationId, CancellationToken.None);

            var groupName = GetConversationGroupName(conversationId);
            await Groups.AddToGroupAsync(Context.ConnectionId, groupName);

            _logger.LogInformation(
                "User {UserId} (ConnectionId: {ConnectionId}) joined conversation group {GroupName}",
                userId, Context.ConnectionId, groupName);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "User {UserId} unauthorized to join conversation {ConversationId}",
                _userContext.UserId, conversationId);
            throw new HubException("You are not authorized to join this conversation.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error joining conversation {ConversationId}", conversationId);
            throw new HubException("Failed to join conversation.");
        }
    }

    /// <summary>
    /// Leave a conversation group.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation to leave.</param>
    public async Task LeaveConversation(Guid conversationId)
    {
        try
        {
            var groupName = GetConversationGroupName(conversationId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);

            _logger.LogInformation(
                "User {UserId} (ConnectionId: {ConnectionId}) left conversation group {GroupName}",
                _userContext.UserId, Context.ConnectionId, groupName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error leaving conversation {ConversationId}", conversationId);
            throw new HubException("Failed to leave conversation.");
        }
    }

    /// <summary>
    /// Send a message in a conversation.
    /// Validates participant access, saves the message, and broadcasts to all participants.
    /// </summary>
    /// <param name="conversationId">The ID of the conversation.</param>
    /// <param name="content">The message content.</param>
    public async Task SendMessage(Guid conversationId, string content)
    {
        try
        {
            var userId = _userContext.UserId;
            if (userId == null)
            {
                _logger.LogWarning("Unauthenticated user attempted to send message in conversation {ConversationId}",
                    conversationId);
                throw new HubException("User is not authenticated.");
            }

            if (string.IsNullOrWhiteSpace(content))
            {
                throw new HubException("Message content cannot be empty.");
            }

            if (content.Length > 4000)
            {
                throw new HubException("Message content is too long (max 4000 characters).");
            }

            // Save message via service (includes validation)
            var messageDto = await _chatService.SendMessageAsync(
                conversationId,
                content,
                CancellationToken.None);

            // Broadcast to all participants in the conversation group
            var groupName = GetConversationGroupName(conversationId);
            await Clients.Group(groupName).SendAsync("ReceiveMessage", messageDto);

            _logger.LogInformation(
                "User {UserId} sent message {MessageId} to conversation {ConversationId}",
                userId, messageDto.Id, conversationId);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "User {UserId} unauthorized to send message in conversation {ConversationId}",
                _userContext.UserId, conversationId);
            throw new HubException("You are not authorized to send messages in this conversation.");
        }
        catch (HubException)
        {
            throw; // Re-throw HubExceptions as-is
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message in conversation {ConversationId}", conversationId);
            throw new HubException("Failed to send message.");
        }
    }

    /// <summary>
    /// Get the SignalR group name for a conversation.
    /// </summary>
    private static string GetConversationGroupName(Guid conversationId)
    {
        return $"conversation-{conversationId}";
    }
}
