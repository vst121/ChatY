namespace ChatY.Core.Entities;

public class Chat
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = string.Empty;
    public ChatType Type { get; set; }
    public string? Description { get; set; }
    public string? PhotoUrl { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public string? CreatedByUserId { get; set; }
    
    // Group chat specific
    public int? MaxParticipants { get; set; }
    public string? JoinLink { get; set; }
    public string? JoinQrCode { get; set; }
    public bool IsPublic { get; set; }
    public bool IsAnnouncementChannel { get; set; }
    
    // Encryption
    public bool IsEndToEndEncrypted { get; set; }
    public string? EncryptionKeyId { get; set; }
    
    // Organization
    public string? FolderId { get; set; }
    public bool IsPinned { get; set; }
    public bool IsArchived { get; set; }
    public int UnreadCount { get; set; }
    public DateTime? LastMessageAt { get; set; }
    
    // Navigation properties
    public ICollection<ChatParticipant> Participants { get; set; } = new List<ChatParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<PinnedMessage> PinnedMessages { get; set; } = new List<PinnedMessage>();
    public ChatFolder? Folder { get; set; }
}

public enum ChatType
{
    Private,
    Group,
    Channel
}


