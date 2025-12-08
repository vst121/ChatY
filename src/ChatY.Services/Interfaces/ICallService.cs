using ChatY.Core.Entities;

namespace ChatY.Services.Interfaces;

public interface ICallService
{
    Task<Call> StartCallAsync(string chatId, string initiatorId, CallType callType);
    Task<Call?> GetCallByIdAsync(string callId);
    Task<Call?> GetActiveCallForChatAsync(string chatId);
    Task<bool> JoinCallAsync(string callId, string userId);
    Task<bool> LeaveCallAsync(string callId, string userId);
    Task<bool> EndCallAsync(string callId);
    Task<bool> UpdateCallParticipantAsync(string callId, string userId, bool isMuted, bool isVideoEnabled);
    Task<IEnumerable<CallParticipant>> GetCallParticipantsAsync(string callId);
    Task<bool> ToggleMuteAsync(string callId, string userId);
    Task<bool> ToggleVideoAsync(string callId, string userId);
    Task<bool> ToggleScreenShareAsync(string callId, string userId);
}