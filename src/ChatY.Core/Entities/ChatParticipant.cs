namespace ChatY.Core.Entities;

public class ChatParticipant
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ChatId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public ParticipantRole Role { get; set; } = ParticipantRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastReadAt { get; set; }
    public bool IsMuted { get; set; }
    public bool IsArchived { get; set; }
    public int UnreadCount { get; set; }
    
    // Navigation properties
    public Chat Chat { get; set; } = null!;
    public User User { get; set; } = null!;
}

public enum ParticipantRole
{
    Owner,
    Admin,
    Moderator,
    Member
}


