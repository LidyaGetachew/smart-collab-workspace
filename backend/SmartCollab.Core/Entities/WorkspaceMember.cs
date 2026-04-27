using System;

namespace SmartCollab.Core.Entities;

public class WorkspaceMember
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid WorkspaceId { get; set; }
    public Guid UserId { get; set; }
    public string Role { get; set; } = "Member";
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Workspace Workspace { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}