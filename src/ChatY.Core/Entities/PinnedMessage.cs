namespace ChatY.Core.Entities;

public class PinnedMessage
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ChatId { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public string PinnedByUserId { get; set; } = string.Empty;
    public DateTime PinnedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Chat Chat { get; set; } = null!;
    public Message Message { get; set; } = null!;
}


