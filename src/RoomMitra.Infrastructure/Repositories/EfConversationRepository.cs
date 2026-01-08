using Microsoft.EntityFrameworkCore;
using RoomMitra.Application.Abstractions.Chat;
using RoomMitra.Domain.Entities;
using RoomMitra.Infrastructure.Persistence;

namespace RoomMitra.Infrastructure.Repositories;

internal sealed class EfConversationRepository : IConversationRepository
{
    private readonly RoomMitraDbContext _db;

    public EfConversationRepository(RoomMitraDbContext db)
    {
        _db = db;
    }

    public Task<Conversation?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return _db.Conversations
            .Include(c => c.Property)
            .Include(c => c.Messages)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
    }

    public async Task<Conversation> GetOrCreateAsync(
        Guid propertyId,
        Guid propertyOwnerId,
        Guid interestedUserId,
        CancellationToken cancellationToken)
    {
        // Try to find existing conversation
        var existing = await _db.Conversations
            .Include(c => c.Property)
            .FirstOrDefaultAsync(
                c => c.PropertyId == propertyId
                     && c.PropertyOwnerId == propertyOwnerId
                     && c.InterestedUserId == interestedUserId,
                cancellationToken);

        if (existing != null)
        {
            return existing;
        }

        // Create new conversation
        var conversation = new Conversation
        {
            PropertyId = propertyId,
            PropertyOwnerId = propertyOwnerId,
            InterestedUserId = interestedUserId
        };

        _db.Conversations.Add(conversation);
        await _db.SaveChangesAsync(cancellationToken);

        // Reload with property
        return await _db.Conversations
            .Include(c => c.Property)
            .FirstAsync(c => c.Id == conversation.Id, cancellationToken);
    }

    public async Task<List<Conversation>> GetUserConversationsAsync(
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _db.Conversations
            .Include(c => c.Property)
            .Include(c => c.Messages.OrderByDescending(m => m.CreatedAt).Take(1))
            .Where(c => c.PropertyOwnerId == userId || c.InterestedUserId == userId)
            .OrderByDescending(c => c.LastMessageAt ?? c.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<Message>> GetConversationMessagesAsync(
        Guid conversationId,
        CancellationToken cancellationToken)
    {
        return await _db.Messages
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.CreatedAt)
            .ToListAsync(cancellationToken);
    }

    public async Task<Message> AddMessageAsync(
        Guid conversationId,
        Guid senderId,
        string senderName,
        string content,
        CancellationToken cancellationToken)
    {
        var message = new Message
        {
            ConversationId = conversationId,
            SenderId = senderId,
            SenderName = senderName,
            Content = content,
            IsRead = false
        };

        _db.Messages.Add(message);
        await _db.SaveChangesAsync(cancellationToken);

        return message;
    }

    public async Task MarkMessagesAsReadAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        var unreadMessages = await _db.Messages
            .Where(m => m.ConversationId == conversationId
                        && m.SenderId != userId
                        && !m.IsRead)
            .ToListAsync(cancellationToken);

        foreach (var message in unreadMessages)
        {
            message.IsRead = true;
        }

        if (unreadMessages.Any())
        {
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task UpdateLastMessageAsync(
        Guid conversationId,
        string content,
        DateTimeOffset timestamp,
        CancellationToken cancellationToken)
    {
        var conversation = await _db.Conversations
            .FirstOrDefaultAsync(c => c.Id == conversationId, cancellationToken);

        if (conversation != null)
        {
            conversation.LastMessageContent = content.Length > 500 ? content[..500] : content;
            conversation.LastMessageAt = timestamp;
            await _db.SaveChangesAsync(cancellationToken);
        }
    }

    public async Task<string> GetUserDisplayNameAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.Id == userId, cancellationToken);
        return user?.Name ?? user?.Email ?? "Unknown User";
    }

    public async Task<int> CountUnreadMessagesAsync(
        Guid conversationId,
        Guid userId,
        CancellationToken cancellationToken)
    {
        return await _db.Messages
            .Where(m => m.ConversationId == conversationId
                        && m.SenderId != userId
                        && !m.IsRead)
            .CountAsync(cancellationToken);
    }

    public async Task<(Guid OwnerId, string Title)?> GetPropertyInfoAsync(
        Guid propertyId,
        CancellationToken cancellationToken)
    {
        var property = await _db.Properties
            .Where(p => p.Id == propertyId)
            .Select(p => new { p.UserId, p.Title })
            .FirstOrDefaultAsync(cancellationToken);

        return property != null ? (property.UserId, property.Title) : null;
    }
}
