namespace ChatY.Core.Entities;

public class MessageReaction
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MessageId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string Emoji { get; set; } = string.Empty;
    public DateTime ReactedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Message Message { get; set; } = null!;
    public User User { get; set; } = null!;
}


