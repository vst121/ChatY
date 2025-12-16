using ChatY.Core.Entities;
using ChatY.Services;
using ChatY.Services.Interfaces;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;

namespace ChatY.Server.Hubs;

public class ChatHub : Hub
{
    private readonly IMessageService _messageService;
    private readonly IChatService _chatService;
    private readonly IUserService _userService;
    private readonly ICallService _callService;
    private readonly ILogger<ChatHub> _logger;
    private static readonly Dictionary<string, string> _userConnections = new();

    private readonly AuthenticationStateService _authState;

    public ChatHub(
        IMessageService messageService,
        IChatService chatService,
        IUserService userService,
        ICallService callService,
        AuthenticationStateService authState,
        ILogger<ChatHub> logger)
    {
        _messageService = messageService;
        _chatService = chatService;
        _userService = userService;
        _callService = callService;
        _authState = authState;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        var userId = _authState.GetCurrentUserId();
        _userConnections[Context.ConnectionId] = userId;
        await _userService.UpdateUserStatusAsync(userId, UserStatus.Online);
        await Clients.All.SendAsync("UserStatusChanged", userId, UserStatus.Online.ToString());
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var userId = _authState.GetCurrentUserId();
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
        var userId = _authState.GetCurrentUserId();
        _logger.LogInformation("User {UserId} joined chat {ChatId}", userId, chatId);
    }

    public async Task LeaveChat(string chatId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, chatId);
        var userId = _authState.GetCurrentUserId();
        _logger.LogInformation("User {UserId} left chat {ChatId}", userId, chatId);
    }

    public async Task SendMessage(string chatId, string content, string messageType = "Text")
    {
        var userId = _authState.GetCurrentUserId();
        if (userId == null) return;

        _logger.LogInformation("Hub received SendMessage: ChatId={ChatId}, UserId={UserId}, Content={Content}", chatId, userId, content);

        try
        {
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
        catch (ArgumentException ex) when (ex.Message == "Chat not found")
        {
            _logger.LogWarning("Attempted to send message to non-existent chat {ChatId} by user {UserId}", chatId, userId);
            await Clients.Caller.SendAsync("Error", "The chat no longer exists.");
        }
        catch (UnauthorizedAccessException ex)
        {
            _logger.LogWarning("Unauthorized message send attempt by user {UserId} in chat {ChatId}: {Message}", userId, chatId, ex.Message);
            await Clients.Caller.SendAsync("Error", "You are not authorized to send messages in this chat.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error sending message in chat {ChatId} by user {UserId}", chatId, userId);
            await Clients.Caller.SendAsync("Error", "An unexpected error occurred while sending the message.");
        }
    }

    public async Task StartTyping(string chatId)
    {
        var userId = _authState.GetCurrentUserId();
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
        var userId = _authState.GetCurrentUserId();
        if (userId == null) return;

        await Clients.GroupExcept(chatId, Context.ConnectionId).SendAsync("UserStoppedTyping", new
        {
            ChatId = chatId,
            UserId = userId
        });
    }

    public async Task AddReaction(string messageId, string emoji)
    {
        var userId = _authState.GetCurrentUserId();
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
        var userId = _authState.GetCurrentUserId();
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
        var userId = _authState.GetCurrentUserId();
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

    // Call methods
    public async Task StartCall(string chatId, string callType)
    {
        var userId = _authState.GetCurrentUserId();
        if (userId == null) return;

        var type = Enum.Parse<CallType>(callType);
        var call = await _callService.StartCallAsync(chatId, userId, type);

        await Clients.Group(chatId).SendAsync("CallStarted", new
        {
            call.Id,
            call.ChatId,
            call.InitiatorId,
            CallType = call.Type.ToString(),
            call.Status,
            call.StartedAt,
            Participants = call.Participants.Select(p => new
            {
                p.Id,
                p.UserId,
                p.IsMuted,
                p.IsVideoEnabled,
                p.IsScreenSharing,
                p.JoinedAt
            })
        });

        _logger.LogInformation("Call {CallId} started by user {UserId} in chat {ChatId}", call.Id, userId, chatId);
    }

    public async Task JoinCall(string callId)
    {
        var userId = _authState.GetCurrentUserId();
        if (userId == null) return;

        var success = await _callService.JoinCallAsync(callId, userId);
        if (!success) return;

        var call = await _callService.GetCallByIdAsync(callId);
        if (call == null) return;

        await Clients.Group(call.ChatId).SendAsync("CallParticipantJoined", new
        {
            CallId = callId,
            UserId = userId,
            Participants = call.Participants.Where(p => p.LeftAt == null).Select(p => new
            {
                p.Id,
                p.UserId,
                p.IsMuted,
                p.IsVideoEnabled,
                p.IsScreenSharing,
                p.JoinedAt
            })
        });

        _logger.LogInformation("User {UserId} joined call {CallId}", userId, callId);
    }

    public async Task LeaveCall(string callId)
    {
        var userId = _authState.GetCurrentUserId();
        if (userId == null) return;

        var success = await _callService.LeaveCallAsync(callId, userId);
        if (!success) return;

        var call = await _callService.GetCallByIdAsync(callId);
        if (call == null) return;

        await Clients.Group(call.ChatId).SendAsync("CallParticipantLeft", new
        {
            CallId = callId,
            UserId = userId,
            CallEnded = call.Status == CallStatus.Ended,
            Participants = call.Participants.Where(p => p.LeftAt == null).Select(p => new
            {
                p.Id,
                p.UserId,
                p.IsMuted,
                p.IsVideoEnabled,
                p.IsScreenSharing,
                p.JoinedAt
            })
        });

        _logger.LogInformation("User {UserId} left call {CallId}", userId, callId);
    }

    public async Task EndCall(string callId)
    {
        var userId = _authState.GetCurrentUserId();
        if (userId == null) return;

        var success = await _callService.EndCallAsync(callId);
        if (!success) return;

        var call = await _callService.GetCallByIdAsync(callId);
        if (call == null) return;

        await Clients.Group(call.ChatId).SendAsync("CallEnded", new
        {
            CallId = callId,
            EndedAt = call.EndedAt,
            Duration = call.Duration
        });

        _logger.LogInformation("Call {CallId} ended by user {UserId}", callId, userId);
    }

    public async Task ToggleMute(string callId)
    {
        var userId = _authState.GetCurrentUserId();
        if (userId == null) return;

        var success = await _callService.ToggleMuteAsync(callId, userId);
        if (!success) return;

        var call = await _callService.GetCallByIdAsync(callId);
        if (call == null) return;

        var participant = call.Participants.FirstOrDefault(p => p.UserId == userId && p.LeftAt == null);
        if (participant == null) return;

        await Clients.Group(call.ChatId).SendAsync("ParticipantMuted", new
        {
            CallId = callId,
            UserId = userId,
            IsMuted = participant.IsMuted
        });
    }

    public async Task ToggleVideo(string callId)
    {
        var userId = _authState.GetCurrentUserId();
        if (userId == null) return;

        var success = await _callService.ToggleVideoAsync(callId, userId);
        if (!success) return;

        var call = await _callService.GetCallByIdAsync(callId);
        if (call == null) return;

        var participant = call.Participants.FirstOrDefault(p => p.UserId == userId && p.LeftAt == null);
        if (participant == null) return;

        await Clients.Group(call.ChatId).SendAsync("ParticipantVideoToggled", new
        {
            CallId = callId,
            UserId = userId,
            IsVideoEnabled = participant.IsVideoEnabled
        });
    }

    public async Task ToggleScreenShare(string callId)
    {
        var userId = _authState.GetCurrentUserId();
        if (userId == null) return;

        var success = await _callService.ToggleScreenShareAsync(callId, userId);
        if (!success) return;

        var call = await _callService.GetCallByIdAsync(callId);
        if (call == null) return;

        var participant = call.Participants.FirstOrDefault(p => p.UserId == userId && p.LeftAt == null);
        if (participant == null) return;

        await Clients.Group(call.ChatId).SendAsync("ParticipantScreenShareToggled", new
        {
            CallId = callId,
            UserId = userId,
            IsScreenSharing = participant.IsScreenSharing
        });
    }

    // WebRTC signaling methods
    public async Task SendOffer(string callId, string targetUserId, string offer)
    {
        var userId = _authState.GetCurrentUserId();
        if (userId == null) return;

        await Clients.User(targetUserId).SendAsync("ReceiveOffer", new
        {
            CallId = callId,
            FromUserId = userId,
            Offer = offer
        });
    }

    public async Task SendAnswer(string callId, string targetUserId, string answer)
    {
        var userId = _authState.GetCurrentUserId();
        if (userId == null) return;

        await Clients.User(targetUserId).SendAsync("ReceiveAnswer", new
        {
            CallId = callId,
            FromUserId = userId,
            Answer = answer
        });
    }

    public async Task SendIceCandidate(string callId, string targetUserId, string candidate)
    {
        var userId = _authState.GetCurrentUserId();
        if (userId == null) return;

        await Clients.User(targetUserId).SendAsync("ReceiveIceCandidate", new
        {
            CallId = callId,
            FromUserId = userId,
            Candidate = candidate
        });
    }
}


