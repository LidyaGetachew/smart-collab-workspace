using System;
using System.Collections.Generic;

namespace SmartCollab.Core.Entities;

public class User
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Role { get; set; } = "Member";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastLoginAt { get; set; }

    // Navigation properties
    public virtual ICollection<Workspace> OwnedWorkspaces { get; set; } = new List<Workspace>();
    public virtual ICollection<WorkspaceMember> WorkspaceMembers { get; set; } = new List<WorkspaceMember>();
    public virtual ICollection<TaskItem> AssignedTasks { get; set; } = new List<TaskItem>();
    public virtual ICollection<TaskItem> CreatedTasks { get; set; } = new List<TaskItem>();
    public virtual ICollection<FileEntity> UploadedFiles { get; set; } = new List<FileEntity>();
}