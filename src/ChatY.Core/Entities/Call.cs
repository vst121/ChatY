namespace ChatY.Core.Entities;

public class Call
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ChatId { get; set; } = string.Empty;
    public string InitiatorId { get; set; } = string.Empty;
    public CallType Type { get; set; }
    public CallStatus Status { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? EndedAt { get; set; }
    public int? Duration { get; set; } // in seconds
    public bool IsScreenSharing { get; set; }
    public bool HasBackgroundBlur { get; set; }
    
    // Navigation properties
    public ICollection<CallParticipant> Participants { get; set; } = new List<CallParticipant>();
}

public enum CallType
{
    Voice,
    Video,
    AudioRoom
}

public enum CallStatus
{
    Ringing,
    InProgress,
    Ended,
    Missed,
    Rejected,
    Cancelled
}

public class CallParticipant
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string CallId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public bool IsMuted { get; set; }
    public bool IsVideoEnabled { get; set; }
    public bool IsScreenSharing { get; set; }
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LeftAt { get; set; }
    
    // Navigation property
    public Call Call { get; set; } = null!;
}


