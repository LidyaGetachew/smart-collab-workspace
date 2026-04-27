using System;

namespace SmartCollab.Core.Entities;

public class ChatMessage
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public Guid WorkspaceId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public string Message { get; set; } = string.Empty;
    public string MessageType { get; set; } = "text";
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }

    // Navigation properties
    public virtual Workspace Workspace { get; set; } = null!;
    public virtual User User { get; set; } = null!;
}