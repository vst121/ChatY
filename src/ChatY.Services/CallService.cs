using ChatY.Core.Entities;
using ChatY.Infrastructure.Data;
using ChatY.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ChatY.Services;

public class CallService : ICallService
{
    private readonly ChatYDbContext _context;
    private readonly ILogger<CallService> _logger;

    public CallService(ChatYDbContext context, ILogger<CallService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<Call> StartCallAsync(string chatId, string initiatorId, CallType callType)
    {
        // Check if there's already an active call for this chat
        var existingCall = await GetActiveCallForChatAsync(chatId);
        if (existingCall != null)
        {
            throw new InvalidOperationException("A call is already active for this chat");
        }

        var call = new Call
        {
            ChatId = chatId,
            InitiatorId = initiatorId,
            Type = callType,
            Status = CallStatus.Ringing,
            StartedAt = DateTime.UtcNow
        };

        _context.Calls.Add(call);

        // Add initiator as participant
        var initiatorParticipant = new CallParticipant
        {
            CallId = call.Id,
            UserId = initiatorId,
            JoinedAt = DateTime.UtcNow,
            IsMuted = false,
            IsVideoEnabled = callType == CallType.Video,
            IsScreenSharing = false
        };

        _context.CallParticipants.Add(initiatorParticipant);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Call {CallId} started by user {UserId} in chat {ChatId}", call.Id, initiatorId, chatId);
        return call;
    }

    public async Task<Call?> GetCallByIdAsync(string callId)
    {
        return await _context.Calls
            .Include(c => c.Participants)
            .FirstOrDefaultAsync(c => c.Id == callId);
    }

    public async Task<Call?> GetActiveCallForChatAsync(string chatId)
    {
        return await _context.Calls
            .Include(c => c.Participants)
            .Where(c => c.ChatId == chatId && c.Status == CallStatus.InProgress)
            .FirstOrDefaultAsync();
    }

    public async Task<bool> JoinCallAsync(string callId, string userId)
    {
        var call = await _context.Calls.FindAsync(callId);
        if (call == null || call.Status != CallStatus.Ringing && call.Status != CallStatus.InProgress)
        {
            return false;
        }

        var existingParticipant = await _context.CallParticipants
            .FirstOrDefaultAsync(cp => cp.CallId == callId && cp.UserId == userId);

        if (existingParticipant != null)
        {
            // User is already in the call
            return true;
        }

        var participant = new CallParticipant
        {
            CallId = callId,
            UserId = userId,
            JoinedAt = DateTime.UtcNow,
            IsMuted = false,
            IsVideoEnabled = call.Type == CallType.Video,
            IsScreenSharing = false
        };

        _context.CallParticipants.Add(participant);

        // Update call status to InProgress if it was ringing
        if (call.Status == CallStatus.Ringing)
        {
            call.Status = CallStatus.InProgress;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} joined call {CallId}", userId, callId);
        return true;
    }

    public async Task<bool> LeaveCallAsync(string callId, string userId)
    {
        var participant = await _context.CallParticipants
            .FirstOrDefaultAsync(cp => cp.CallId == callId && cp.UserId == userId && cp.LeftAt == null);

        if (participant == null)
        {
            return false;
        }

        participant.LeftAt = DateTime.UtcNow;

        // Check if this was the last participant
        var activeParticipants = await _context.CallParticipants
            .CountAsync(cp => cp.CallId == callId && cp.LeftAt == null);

        if (activeParticipants <= 1)
        {
            // End the call
            var call = await _context.Calls.FindAsync(callId);
            if (call != null)
            {
                call.Status = CallStatus.Ended;
                call.EndedAt = DateTime.UtcNow;
                call.Duration = (int)(DateTime.UtcNow - call.StartedAt).TotalSeconds;
            }
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("User {UserId} left call {CallId}", userId, callId);
        return true;
    }

    public async Task<bool> EndCallAsync(string callId)
    {
        var call = await _context.Calls.FindAsync(callId);
        if (call == null || call.Status == CallStatus.Ended)
        {
            return false;
        }

        call.Status = CallStatus.Ended;
        call.EndedAt = DateTime.UtcNow;
        call.Duration = (int)(DateTime.UtcNow - call.StartedAt).TotalSeconds;

        // Mark all participants as left
        var participants = await _context.CallParticipants
            .Where(cp => cp.CallId == callId && cp.LeftAt == null)
            .ToListAsync();

        foreach (var participant in participants)
        {
            participant.LeftAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();

        _logger.LogInformation("Call {CallId} ended", callId);
        return true;
    }

    public async Task<bool> UpdateCallParticipantAsync(string callId, string userId, bool isMuted, bool isVideoEnabled)
    {
        var participant = await _context.CallParticipants
            .FirstOrDefaultAsync(cp => cp.CallId == callId && cp.UserId == userId && cp.LeftAt == null);

        if (participant == null)
        {
            return false;
        }

        participant.IsMuted = isMuted;
        participant.IsVideoEnabled = isVideoEnabled;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<CallParticipant>> GetCallParticipantsAsync(string callId)
    {
        return await _context.CallParticipants
            .Include(cp => cp.Call)
            .Where(cp => cp.CallId == callId && cp.LeftAt == null)
            .ToListAsync();
    }

    public async Task<bool> ToggleMuteAsync(string callId, string userId)
    {
        var participant = await _context.CallParticipants
            .FirstOrDefaultAsync(cp => cp.CallId == callId && cp.UserId == userId && cp.LeftAt == null);

        if (participant == null)
        {
            return false;
        }

        participant.IsMuted = !participant.IsMuted;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleVideoAsync(string callId, string userId)
    {
        var participant = await _context.CallParticipants
            .FirstOrDefaultAsync(cp => cp.CallId == callId && cp.UserId == userId && cp.LeftAt == null);

        if (participant == null)
        {
            return false;
        }

        participant.IsVideoEnabled = !participant.IsVideoEnabled;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleScreenShareAsync(string callId, string userId)
    {
        var participant = await _context.CallParticipants
            .FirstOrDefaultAsync(cp => cp.CallId == callId && cp.UserId == userId && cp.LeftAt == null);

        if (participant == null)
        {
            return false;
        }

        participant.IsScreenSharing = !participant.IsScreenSharing;
        await _context.SaveChangesAsync();
        return true;
    }
}