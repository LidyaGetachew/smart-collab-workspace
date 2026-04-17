using System;

namespace SmartCollab.Core.Entities;

public class TaskItem
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = "Todo";
    public int Priority { get; set; } = 2;
    public Guid WorkspaceId { get; set; }
    public Guid? AssignedToId { get; set; }
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? DueDate { get; set; }
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual Workspace Workspace { get; set; } = null!;
    public virtual User AssignedTo { get; set; } = null!;
    public virtual User CreatedBy { get; set; } = null!;
}