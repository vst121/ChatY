namespace ChatY.Core.Entities;

public class TaskItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string ChatId { get; set; } = string.Empty;
    public string CreatedByUserId { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public TaskStatus Status { get; set; } = TaskStatus.Pending;
    public DateTime? DueDate { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    
    // Navigation properties
    public ICollection<TaskAssignee> Assignees { get; set; } = new List<TaskAssignee>();
}

public enum TaskStatus
{
    Pending,
    InProgress,
    Completed,
    Cancelled
}

public class TaskAssignee
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string TaskId { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    
    // Navigation property
    public TaskItem Task { get; set; } = null!;
}


