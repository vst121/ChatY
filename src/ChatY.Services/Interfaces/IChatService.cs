using ChatY.Core.Entities;

namespace ChatY.Services.Interfaces;

public interface IChatService
{
    Task<Chat> CreateChatAsync(string userId, string name, ChatType type, IEnumerable<string> participantIds);
    Task<Chat?> GetChatByIdAsync(string chatId, string userId);
    Task<IEnumerable<Chat>> GetUserChatsAsync(string userId);
    Task<Chat> AddParticipantAsync(string chatId, string userId, string addedByUserId);
    Task RemoveParticipantAsync(string chatId, string userId);
    Task UpdateChatAsync(Chat chat);
    Task DeleteChatAsync(string chatId);
    Task PinChatAsync(string chatId, string userId);
    Task UnpinChatAsync(string chatId, string userId);
    Task ArchiveChatAsync(string chatId, string userId);
    Task MarkAsReadAsync(string chatId, string userId);
}


