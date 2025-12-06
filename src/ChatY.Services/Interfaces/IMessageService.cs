using ChatY.Core.Entities;

namespace ChatY.Services.Interfaces;

public interface IMessageService
{
    Task<Message> SendMessageAsync(string chatId, string senderId, string content, MessageType type);
    Task<Message?> GetMessageByIdAsync(string messageId);
    Task<IEnumerable<Message>> GetChatMessagesAsync(string chatId, int skip = 0, int take = 50);
    Task<Message> EditMessageAsync(string messageId, string newContent, string userId);
    Task DeleteMessageAsync(string messageId, string userId);
    Task<MessageReaction> AddReactionAsync(string messageId, string userId, string emoji);
    Task RemoveReactionAsync(string messageId, string userId, string emoji);
    Task<Message> ReplyToMessageAsync(string chatId, string senderId, string parentMessageId, string content);
    Task PinMessageAsync(string chatId, string messageId, string userId);
    Task MarkMessageAsReadAsync(string messageId, string userId);
    Task MarkMessageAsDeliveredAsync(string messageId, string userId);
}


