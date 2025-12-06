namespace ChatY.Core.Entities;

public class Poll
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ChatId { get; set; } = string.Empty;
    public string MessageId { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public string Question { get; set; } = string.Empty;
    public bool IsMultipleChoice { get; set; }
    public bool IsAnonymous { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public ICollection<PollOption> Options { get; set; } = new List<PollOption>();
    public ICollection<PollVote> Votes { get; set; } = new List<PollVote>();
}

public class PollOption
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PollId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public int VoteCount { get; set; }
    public int Order { get; set; }
    
    // Navigation property
    public Poll Poll { get; set; } = null!;
}

public class PollVote
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string PollId { get; set; } = string.Empty;
    public string OptionId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime VotedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation properties
    public Poll Poll { get; set; } = null!;
    public PollOption Option { get; set; } = null!;
}


