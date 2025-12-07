using ChatY.Core.Entities;
using ChatY.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace ChatY.Server.Hubs;

public class ChatHub : Hub
{
    private readonly IMessageService _messageService;
    private readonly IChatService _chatService;
    private readonly IUserService _userService;
    private readonly ILogger<ChatHub> _logger;
    private static readonly Dictionary<string, string> _userConnections = new();

    public ChatHub(
        IMessageService messageService,
        IChatService chatService,
        IUserService userService,
        ILogger<ChatHub> logger)
    {
        _messageService = messageService;
        _chatService = chatService;
        _userService = userService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = Context.UserIdentifier ?? "user1"; // Default to user1 for testing
        _userConnections[Context.ConnectionId] = userId;
        await _userService.UpdateUserStatusAsync(userId, UserStatus.Online);
        await Clients.All.SendAsync("UserStatusChanged", userId, UserStatus.Online.ToString());
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = Context.UserIdentifier ?? "user1"; // Default to user1 for testing
        if (_userConnections.ContainsKey(Context.ConnectionId))
        {
            _userConnections.Remove(Context.ConnectionId);

            // Check if user has other connections
            var hasOtherConnections = _userConnections.Values.Any(v => v == userId);
            if (!hasOtherConnections)
            {
                await _userService.UpdateUserStatusAsync(userId, UserStatus.Offline);
                await Clients.All.SendAsync("UserStatusChanged", userId, UserStatus.Offline.ToString());
            }
        }
        await base.OnDisconnectedAsync(exception);
    }

    public async Task JoinChat(string chatId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, chatId);
        _logger.LogInformation("User {UserId} joined chat {ChatId}", Context.UserIdentifier, chatId);
    }

    public async Task LeaveChat(string chatId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId);
        _logger.LogInformation("User {UserId} left chat {ChatId}", Context.UserIdentifier, chatId);
    }

    public async Task SendMessage(string chatId, string content, string messageType = "Text")
    {
        var userId = Context.UserIdentifier ?? "user1"; // Default to user1 for testing
        if (userId == null) return;

        var type = Enum.Parse<MessageType>(messageType);
        var message = await _messageService.SendMessageAsync(chatId, userId, content, type);

        await Clients.Group(chatId).SendAsync("MessageReceived", new
        {
            message.Id,
            message.ChatId,
            message.SenderId,
            message.Content,
            message.Type,
            message.SentAt,
            Sender = new { message.Sender.UserName, message.Sender.DisplayName, message.Sender.ProfilePhotoUrl }
        });
    }

    public async Task StartTyping(string chatId)
    {
        var userId = Context.UserIdentifier ?? "user1"; // Default to user1 for testing
        if (userId == null) return;

        var user = await _userService.GetUserByIdAsync(userId);
        if (user == null) return;

        await Clients.GroupExcept(chatId, Context.ConnectionId).SendAsync("UserTyping", new
        {
            ChatId = chatId,
            UserId = userId,
            UserName = user.DisplayName ?? user.UserName
        });
    }

    public async Task StopTyping(string chatId)
    {
        var userId = Context.UserIdentifier ?? "user1"; // Default to user1 for testing
        if (userId == null) return;

        await Clients.GroupExcept(chatId, Context.ConnectionId).SendAsync("UserStoppedTyping", new
        {
            ChatId = chatId,
            UserId = userId
        });
    }

    public async Task AddReaction(string messageId, string emoji)
    {
        var userId = Context.UserIdentifier ?? "user1"; // Default to user1 for testing
        if (userId == null) return;

        var reaction = await _messageService.AddReactionAsync(messageId, userId, emoji);
        var message = await _messageService.GetMessageByIdAsync(messageId);
        
        if (message != null)
        {
            await Clients.Group(message.ChatId).SendAsync("ReactionAdded", new
            {
                MessageId = messageId,
                Reaction = new { reaction.Id, reaction.Emoji, reaction.UserId }
            });
        }
    }

    public async Task RemoveReaction(string messageId, string emoji)
    {
        var userId = Context.UserIdentifier ?? "user1"; // Default to user1 for testing
        if (userId == null) return;

        await _messageService.RemoveReactionAsync(messageId, userId, emoji);
        var message = await _messageService.GetMessageByIdAsync(messageId);
        
        if (message != null)
        {
            await Clients.Group(message.ChatId).SendAsync("ReactionRemoved", new
            {
                MessageId = messageId,
                UserId = userId,
                Emoji = emoji
            });
        }
    }

    public async Task MarkMessageAsRead(string messageId)
    {
        var userId = Context.UserIdentifier ?? "user1"; // Default to user1 for testing
        if (userId == null) return;

        await _messageService.MarkMessageAsReadAsync(messageId, userId);
        var message = await _messageService.GetMessageByIdAsync(messageId);
        
        if (message != null)
        {
            await Clients.Group(message.ChatId).SendAsync("MessageRead", new
            {
                MessageId = messageId,
                UserId = userId
            });
        }
    }
}


