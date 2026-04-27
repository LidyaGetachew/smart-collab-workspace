using System;

namespace SmartCollab.Core.Entities;

public class Comment
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Content { get; set; } = string.Empty;
    public Guid TaskId { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatar { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public virtual TaskItem Task { get; set; } = null!;
    public virtual User Author { get; set; } = null!;
}