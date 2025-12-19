using Microsoft.AspNetCore.Identity;

namespace ChatY.Core.Entities;

public class User : IdentityUser
{
    public string? DisplayName { get; set; }
    public string? ProfilePhotoUrl { get; set; }
    public string? Status { get; set; }
    public UserStatus UserStatus { get; set; } = UserStatus.Offline;
    public DateTime? LastSeen { get; set; }
    public string? Bio { get; set; }
    public bool IsOnline { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    
    // Privacy settings
    public PrivacySettings PrivacySettings { get; set; } = new();
    
    // Security
    public bool TwoFactorEnabled { get; set; }
    public string? PasskeyCredentialId { get; set; }
    
    // Navigation properties
    public ICollection<ChatParticipant> ChatParticipants { get; set; } = new List<ChatParticipant>();
    public ICollection<Message> Messages { get; set; } = new List<Message>();
    public ICollection<MessageReaction> MessageReactions { get; set; } = new List<MessageReaction>();
    public ICollection<UserBlock> BlockedUsers { get; set; } = new List<UserBlock>();
    public ICollection<UserBlock> BlockedByUsers { get; set; } = new List<UserBlock>();
}

public enum UserStatus
{
    Online,
    Offline,
    Away,
    Busy,
    Invisible
}

public class PrivacySettings
{
    public bool ShowLastSeen { get; set; } = true;
    public bool ShowProfilePhoto { get; set; } = true;
    public bool ShowStatus { get; set; } = true;
    public WhoCanMessageMe WhoCanMessageMe { get; set; } = WhoCanMessageMe.Everyone;
    public WhoCanAddToGroups WhoCanAddToGroups { get; set; } = WhoCanAddToGroups.Everyone;
}

public enum WhoCanMessageMe
{
    Everyone,
    Contacts,
    Nobody
}

public enum WhoCanAddToGroups
{
    Everyone,
    Contacts,
    Nobody
}


