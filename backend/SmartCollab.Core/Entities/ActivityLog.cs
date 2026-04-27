using System;

namespace SmartCollab.Core.Entities;

public class ActivityLog
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid WorkspaceId { get; set; }
    public Guid UserId { get; set; }
    public Guid? TaskId { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Metadata { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Workspace Workspace { get; set; } = null!;
    public virtual User User { get; set; } = null!;
    public virtual TaskItem? Task { get; set; }
}