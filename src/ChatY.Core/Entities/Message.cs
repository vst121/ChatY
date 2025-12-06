namespace ChatY.Core.Entities;

public class Message
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ChatId { get; set; } = string.Empty;
    public string SenderId { get; set; } = string.Empty;
    public string Content { get; set; } = string.Empty;
    public MessageType Type { get; set; } = MessageType.Text;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime? EditedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
    public bool IsDeleted { get; set; }
    public bool IsEdited { get; set; }
    public bool IsPinned { get; set; }
    
    // Threading
    public string? ParentMessageId { get; set; }
    public Message? ParentMessage { get; set; }
    public ICollection<Message> Replies { get; set; } = new List<Message>();
    public int ReplyCount { get; set; }
    
    // Media
    public ICollection<MessageMedia> Media { get; set; } = new List<MessageMedia>();
    public ICollection<MessageFile> Files { get; set; } = new List<MessageFile>();
    
    // Link preview
    public LinkPreview? LinkPreview { get; set; }
    
    // Receipts
    public MessageReceipt Receipt { get; set; } = new();
    
    // Reactions
    public ICollection<MessageReaction> Reactions { get; set; } = new List<MessageReaction>();
    
    // Encryption
    public bool IsEncrypted { get; set; }
    public string? EncryptedContent { get; set; }
    
    // Disappearing messages
    public DateTime? ExpiresAt { get; set; }
    public bool IsDisappearing { get; set; }
    
    // Navigation properties
    public Chat Chat { get; set; } = null!;
    public User Sender { get; set; } = null!;
}

public enum MessageType
{
    Text,
    Image,
    Video,
    Audio,
    VoiceNote,
    File,
    Location,
    Contact,
    Sticker,
    GIF,
    System
}

public class MessageReceipt
{
    public int DeliveredCount { get; set; }
    public int ReadCount { get; set; }
    public ICollection<string> DeliveredToUserIds { get; set; } = new List<string>();
    public ICollection<string> ReadByUserIds { get; set; } = new List<string>();
}


