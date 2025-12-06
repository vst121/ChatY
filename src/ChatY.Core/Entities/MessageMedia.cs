namespace ChatY.Core.Entities;

public class MessageMedia
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string MessageId { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string? ThumbnailUrl { get; set; }
    public string? FileName { get; set; }
    public string? MimeType { get; set; }
    public long FileSize { get; set; }
    public int? Width { get; set; }
    public int? Height { get; set; }
    public int? Duration { get; set; } // For video/audio in seconds
    public string? Caption { get; set; }
    public bool HasAutoCaption { get; set; }
    
    // Navigation property
    public Message Message { get; set; } = null!;
}


