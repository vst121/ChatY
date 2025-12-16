using ChatY.Core.Entities;
using ChatY.Infrastructure.Data;
using ChatY.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatY.Services;

public class MessageService : IMessageService
{
    private readonly ChatYDbContext _context;
    private readonly ILogger<MessageService> _logger;

    public MessageService(ChatYDbContext context, ILogger<MessageService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Message> SendMessageAsync(string chatId, string senderId, string content, MessageType type)
    {
        _logger.LogInformation("Attempting to send message to chat {ChatId} by user {SenderId}", chatId, senderId);

        // Validate chat exists
        var chat = await _context.Chats.FindAsync(chatId);
        if (chat == null)
        {
            _logger.LogWarning("Chat {ChatId} not found for message send by user {SenderId}", chatId, senderId);
            throw new ArgumentException("Chat not found");
        }

        // Validate sender is a participant
        var isParticipant = await _context.ChatParticipants
            .AnyAsync(cp => cp.ChatId == chatId && cp.UserId == senderId);
        if (!isParticipant)
            throw new UnauthorizedAccessException("User is not a participant in this chat");

        var message = new Message
        {
            ChatId = chatId,
            SenderId = senderId,
            Content = content,
            Type = type,
            SentAt = DateTime.UtcNow
        };

        _context.Messages.Add(message);

        // Update chat last message time
        chat.LastMessageAt = DateTime.UtcNow;
        chat.UpdatedAt = DateTime.UtcNow;

        // Increment unread count for all participants except sender
        var participants = await _context.ChatParticipants
            .Where(cp => cp.ChatId == chatId && cp.UserId != senderId)
            .ToListAsync();

        foreach (var participant in participants)
        {
            participant.UnreadCount++;
        }

        await _context.SaveChangesAsync();
        return message;
    }

    public async Task<Message?> GetMessageByIdAsync(string messageId)
    {
        return await _context.Messages
            .Include(m => m.Sender)
            .Include(m => m.Reactions)
            .Include(m => m.Media)
            .Include(m => m.Files)
            .FirstOrDefaultAsync(m => m.Id == messageId);
    }

    public async Task<IEnumerable<Message>> GetChatMessagesAsync(string chatId, int skip = 0, int take = 50)
    {
        return await _context.Messages
            .Where(m => m.ChatId == chatId && !m.IsDeleted)
            .Include(m => m.Sender)
            .Include(m => m.Reactions)
            .Include(m => m.Media)
            .Include(m => m.Files)
            .OrderByDescending(m => m.SentAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    public async Task<Message> EditMessageAsync(string messageId, string newContent, string userId)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message == null || message.SenderId != userId)
            throw new UnauthorizedAccessException("Cannot edit this message");

        message.Content = newContent;
        message.IsEdited = true;
        message.EditedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return message;
    }

    public async Task DeleteMessageAsync(string messageId, string userId)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message == null)
            return;

        // Check if user is sender or admin
        var isParticipant = await _context.ChatParticipants
            .AnyAsync(cp => cp.ChatId == message.ChatId && cp.UserId == userId &&
                           (cp.Role == ParticipantRole.Owner || cp.Role == ParticipantRole.Admin));

        if (message.SenderId != userId && !isParticipant)
            throw new UnauthorizedAccessException("Cannot delete this message");

        message.IsDeleted = true;
        message.DeletedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<MessageReaction> AddReactionAsync(string messageId, string userId, string emoji)
    {
        var existingReaction = await _context.MessageReactions
            .FirstOrDefaultAsync(mr => mr.MessageId == messageId && mr.UserId == userId && mr.Emoji == emoji);

        if (existingReaction != null)
            return existingReaction;

        var reaction = new MessageReaction
        {
            MessageId = messageId,
            UserId = userId,
            Emoji = emoji,
            ReactedAt = DateTime.UtcNow
        };

        _context.MessageReactions.Add(reaction);
        await _context.SaveChangesAsync();
        return reaction;
    }

    public async Task RemoveReactionAsync(string messageId, string userId, string emoji)
    {
        var reaction = await _context.MessageReactions
            .FirstOrDefaultAsync(mr => mr.MessageId == messageId && mr.UserId == userId && mr.Emoji == emoji);

        if (reaction != null)
        {
            _context.MessageReactions.Remove(reaction);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<Message> ReplyToMessageAsync(string chatId, string senderId, string parentMessageId, string content)
    {
        var parentMessage = await _context.Messages.FindAsync(parentMessageId);
        if (parentMessage == null)
            throw new ArgumentException("Parent message not found");

        // Validate chat exists
        var chat = await _context.Chats.FindAsync(chatId);
        if (chat == null)
            throw new ArgumentException("Chat not found");

        // Validate sender is a participant
        var isParticipant = await _context.ChatParticipants
            .AnyAsync(cp => cp.ChatId == chatId && cp.UserId == senderId);
        if (!isParticipant)
            throw new UnauthorizedAccessException("User is not a participant in this chat");

        var reply = new Message
        {
            ChatId = chatId,
            SenderId = senderId,
            Content = content,
            Type = MessageType.Text,
            ParentMessageId = parentMessageId,
            SentAt = DateTime.UtcNow
        };

        _context.Messages.Add(reply);

        // Update parent message reply count
        parentMessage.ReplyCount++;

        // Update chat last message time
        chat.LastMessageAt = DateTime.UtcNow;
        chat.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return reply;
    }

    public async Task PinMessageAsync(string chatId, string messageId, string userId)
    {
        var existingPin = await _context.PinnedMessages
            .FirstOrDefaultAsync(pm => pm.ChatId == chatId && pm.MessageId == messageId);

        if (existingPin != null)
            return;

        var pinnedMessage = new PinnedMessage
        {
            ChatId = chatId,
            MessageId = messageId,
            PinnedByUserId = userId,
            PinnedAt = DateTime.UtcNow
        };

        _context.PinnedMessages.Add(pinnedMessage);

        var message = await _context.Messages.FindAsync(messageId);
        if (message != null)
        {
            message.IsPinned = true;
        }

        await _context.SaveChangesAsync();
    }

    public async Task MarkMessageAsReadAsync(string messageId, string userId)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message == null || message.SenderId == userId)
            return;

        if (!message.Receipt.ReadByUserIds.Contains(userId))
        {
            message.Receipt.ReadByUserIds.Add(userId);
            message.Receipt.ReadCount++;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkMessageAsDeliveredAsync(string messageId, string userId)
    {
        var message = await _context.Messages.FindAsync(messageId);
        if (message == null || message.SenderId == userId)
            return;

        if (!message.Receipt.DeliveredToUserIds.Contains(userId))
        {
            message.Receipt.DeliveredToUserIds.Add(userId);
            message.Receipt.DeliveredCount++;
            await _context.SaveChangesAsync();
        }
    }
}


