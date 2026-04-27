using System;

namespace SmartCollab.Core.Entities;

public class FileEntity
{
    public Guid Id { get; set; } = Guid.CreateVersion7();
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string MimeType { get; set; } = string.Empty;
    public string? FileHash { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid UploadedById { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public virtual Workspace Workspace { get; set; } = null!;
    public virtual User UploadedBy { get; set; } = null!;
}