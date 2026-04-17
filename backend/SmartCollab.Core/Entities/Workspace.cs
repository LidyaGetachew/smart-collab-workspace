using System;
using System.Collections.Generic;

namespace SmartCollab.Core.Entities;

public class Workspace
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual User Owner { get; set; } = null!;
    public virtual ICollection<WorkspaceMember> Members { get; set; } = new List<WorkspaceMember>();
    public virtual ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    public virtual ICollection<FileEntity> Files { get; set; } = new List<FileEntity>();
}