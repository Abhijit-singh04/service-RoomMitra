using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RoomMitra.Application.Abstractions.Chat;
using RoomMitra.Application.Models.Chat;

namespace RoomMitra.Api.Controllers;

/// <summary>
/// Controller for managing chat conversations and messages.
/// </summary>
[ApiController]
[Route("api/conversations")]
[Authorize]
public sealed class ConversationsController : ControllerBase
{
    private readonly IChatService _chatService;
    private readonly ILogger<ConversationsController> _logger;

    public ConversationsController(
        IChatService chatService,
        ILogger<ConversationsController> logger)
    {
        _chatService = chatService;
        _logger = logger;
    }

    /// <summary>
    /// Get all conversations for the current authenticated user.
    /// </summary>
    [HttpGet("my")]
    [ProducesResponseType(typeof(List<ConversationDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetMyConversations(CancellationToken cancellationToken)
    {
        try
        {
            var conversations = await _chatService.GetMyConversationsAsync(cancellationToken);
            return Ok(conversations);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to conversations");
            return Unauthorized(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting user conversations");
            return Problem(
                title: "Failed to retrieve conversations",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get messages for a specific conversation.
    /// Marks unread messages from the other user as read.
    /// </summary>
    [HttpGet("{conversationId:guid}/messages")]
    [ProducesResponseType(typeof(List<MessageDto>), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetConversationMessages(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        try
        {
            var messages = await _chatService.GetConversationMessagesAsync(conversationId, cancellationToken);
            return Ok(messages);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to conversation {ConversationId}", conversationId);
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Conversation {ConversationId} not found", conversationId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting messages for conversation {ConversationId}", conversationId);
            return Problem(
                title: "Failed to retrieve messages",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Get or create a conversation for a flat listing.
    /// Current user is the interested user.
    /// </summary>
    [HttpPost]
    [ProducesResponseType(typeof(ConversationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ConversationDto), StatusCodes.Status201Created)]
    public async Task<IActionResult> GetOrCreateConversation(
        [FromBody] GetOrCreateConversationRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Creating/getting conversation for flat listing {FlatListingId}", request.FlatListingId);
            
            var conversation = await _chatService.GetOrCreateConversationAsync(
                request.FlatListingId,
                cancellationToken);

            _logger.LogInformation("Successfully created/retrieved conversation {ConversationId} for flat listing {FlatListingId}", 
                conversation.Id, request.FlatListingId);

            // Return 200 if existing, 201 if created
            // For simplicity, always return 200 (client doesn't need to distinguish)
            return Ok(conversation);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access when creating conversation for flat listing {FlatListingId}",
                request.FlatListingId);
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Flat listing {FlatListingId} not found or invalid operation", request.FlatListingId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating conversation for flat listing {FlatListingId}", request.FlatListingId);
            return Problem(
                title: "Failed to create conversation",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }

    /// <summary>
    /// Send a message in a conversation.
    /// Current user must be a participant in the conversation.
    /// </summary>
    [HttpPost("{conversationId:guid}/messages")]
    [ProducesResponseType(typeof(MessageDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> SendMessage(
        Guid conversationId,
        [FromBody] SendMessageRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Sending message in conversation {ConversationId}", conversationId);

            var message = await _chatService.SendMessageAsync(
                conversationId,
                request.Content,
                cancellationToken);

            _logger.LogInformation("Message {MessageId} sent successfully in conversation {ConversationId}",
                message.Id, conversationId);

            return Ok(message);
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning(ex, "Unauthorized access to conversation {ConversationId}", conversationId);
            return Unauthorized(new { error = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Conversation {ConversationId} not found or invalid operation", conversationId);
            return NotFound(new { error = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending message in conversation {ConversationId}", conversationId);
            return Problem(
                title: "Failed to send message",
                detail: ex.Message,
                statusCode: StatusCodes.Status500InternalServerError);
        }
    }
}
