namespace ChatY.Core.Entities;

public class UserBlock
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string BlockerUserId { get; set; } = string.Empty;
    public string BlockedUserId { get; set; } = string.Empty;
    public DateTime BlockedAt { get; set; } = DateTime.UtcNow;
    public string? Reason { get; set; }
    
    // Navigation properties
    public User BlockerUser { get; set; } = null!;
    public User BlockedUser { get; set; } = null!;
}


