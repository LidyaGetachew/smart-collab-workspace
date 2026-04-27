namespace SmartCollab.Core.DTOs;

public class FileResponseDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FormattedFileSize { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string FileIcon { get; set; } = string.Empty;
    public string UploadedByName { get; set; } = string.Empty;
    public Guid UploadedById { get; set; }
    public DateTime UploadedAt { get; set; }
    public string UploadedAtRelative { get; set; } = string.Empty;
}

public class FileUploadResponseDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Message { get; set; } = string.Empty;
}