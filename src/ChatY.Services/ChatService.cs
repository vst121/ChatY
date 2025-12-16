using ChatY.Core.Entities;
using ChatY.Infrastructure.Data;
using ChatY.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatY.Services;

public class ChatService : IChatService
{
    private readonly ChatYDbContext _context;
    private readonly ILogger<ChatService> _logger;

    public ChatService(ChatYDbContext context, ILogger<ChatService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Chat> CreateChatAsync(string userId, string name, ChatType type, IEnumerable<string> participantIds)
    {
        var chat = new Chat
        {
            Name = name,
            Type = type,
            CreatedByUserId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Chats.Add(chat);

        // Add creator as participant
        var participants = new List<ChatParticipant>
        {
            new()
            {
                ChatId = chat.Id,
                UserId = userId,
                Role = ParticipantRole.Owner
            }
        };

        // Add other participants
        foreach (var participantId in participantIds.Where(id => id != userId))
        {
            participants.Add(new ChatParticipant
            {
                ChatId = chat.Id,
                UserId = participantId,
                Role = ParticipantRole.Member
            });
        }

        _context.ChatParticipants.AddRange(participants);
        await _context.SaveChangesAsync();

        return chat;
    }

    public async Task<Chat?> GetChatByIdAsync(string chatId, string userId)
    {
        _logger.LogInformation("Getting chat {ChatId} for user {UserId}", chatId, userId);

        var isParticipant = await _context.ChatParticipants
            .AnyAsync(cp => cp.ChatId == chatId && cp.UserId == userId);

        if (!isParticipant)
        {
            _logger.LogWarning("User {UserId} is not a participant in chat {ChatId}", userId, chatId);
            return null;
        }

        var chat = await _context.Chats
            .Include(c => c.Participants)
                .ThenInclude(p => p.User)
            .FirstOrDefaultAsync(c => c.Id == chatId);

        if (chat == null)
        {
            _logger.LogWarning("Chat {ChatId} not found in database", chatId);
        }
        else
        {
            _logger.LogInformation("Chat {ChatId} found: {ChatName}", chatId, chat.Name);
        }

        return chat;
    }

    public async Task<IEnumerable<Chat>> GetUserChatsAsync(string userId)
    {
        return await _context.ChatParticipants
            .Where(cp => cp.UserId == userId && !cp.IsArchived)
            .Include(cp => cp.Chat)
                .ThenInclude(c => c.Participants)
                    .ThenInclude(p => p.User)
            .OrderByDescending(cp => cp.Chat.LastMessageAt ?? cp.Chat.CreatedAt)
            .Select(cp => cp.Chat)
            .ToListAsync();
    }

    public async Task<Chat> AddParticipantAsync(string chatId, string userId, string addedByUserId)
    {
        var chat = await _context.Chats.FindAsync(chatId);
        if (chat == null)
            throw new ArgumentException("Chat not found");

        var existingParticipant = await _context.ChatParticipants
            .FirstOrDefaultAsync(cp => cp.ChatId == chatId && cp.UserId == userId);

        if (existingParticipant != null)
            return chat;

        var participant = new ChatParticipant
        {
            ChatId = chatId,
            UserId = userId,
            Role = ParticipantRole.Member
        };

        _context.ChatParticipants.Add(participant);
        await _context.SaveChangesAsync();

        return chat;
    }

    public async Task RemoveParticipantAsync(string chatId, string userId)
    {
        var participant = await _context.ChatParticipants
            .FirstOrDefaultAsync(cp => cp.ChatId == chatId && cp.UserId == userId);

        if (participant != null)
        {
            _context.ChatParticipants.Remove(participant);
            await _context.SaveChangesAsync();
        }
    }

    public async Task UpdateChatAsync(Chat chat)
    {
        chat.UpdatedAt = DateTime.UtcNow;
        _context.Chats.Update(chat);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteChatAsync(string chatId)
    {
        var chat = await _context.Chats.FindAsync(chatId);
        if (chat != null)
        {
            _context.Chats.Remove(chat);
            await _context.SaveChangesAsync();
        }
    }

    public async Task PinChatAsync(string chatId, string userId)
    {
        var participant = await _context.ChatParticipants
            .FirstOrDefaultAsync(cp => cp.ChatId == chatId && cp.UserId == userId);

        if (participant != null)
        {
            var chat = await _context.Chats.FindAsync(chatId);
            if (chat != null)
            {
                chat.IsPinned = true;
                await _context.SaveChangesAsync();
            }
        }
    }

    public async Task UnpinChatAsync(string chatId, string userId)
    {
        var chat = await _context.Chats.FindAsync(chatId);
        if (chat != null)
        {
            chat.IsPinned = false;
            await _context.SaveChangesAsync();
        }
    }

    public async Task ArchiveChatAsync(string chatId, string userId)
    {
        var participant = await _context.ChatParticipants
            .FirstOrDefaultAsync(cp => cp.ChatId == chatId && cp.UserId == userId);

        if (participant != null)
        {
            participant.IsArchived = true;
            await _context.SaveChangesAsync();
        }
    }

    public async Task MarkAsReadAsync(string chatId, string userId)
    {
        var participant = await _context.ChatParticipants
            .FirstOrDefaultAsync(cp => cp.ChatId == chatId && cp.UserId == userId);

        if (participant != null)
        {
            participant.LastReadAt = DateTime.UtcNow;
            participant.UnreadCount = 0;
            await _context.SaveChangesAsync();
        }
    }
}


