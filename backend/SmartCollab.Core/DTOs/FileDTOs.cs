using System.ComponentModel.DataAnnotations;

namespace SmartCollab.Core.DTOs;

public class FileUploadDto
{
    [Required]
    public string FileName { get; set; } = string.Empty;

    [Required]
    public long FileSize { get; set; }

    [Required]
    public string MimeType { get; set; } = string.Empty;

    public string? Description { get; set; }
}

public class FileResponseDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string FormattedFileSize { get; set; } = string.Empty;
    public string MimeType { get; set; } = string.Empty;
    public string FileIcon { get; set; } = string.Empty;
    public string UploadedByName { get; set; } = string.Empty;
    public Guid UploadedById { get; set; }
    public DateTime UploadedAt { get; set; }
    public string? Description { get; set; }
}

public class FileDownloadDto
{
    public byte[] Content { get; set; } = Array.Empty<byte>();
    public string ContentType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}

public class DeleteFileDto
{
    [Required]
    public Guid FileId { get; set; }
}

public class BatchFileUploadDto
{
    [Required]
    public List<FileUploadDto> Files { get; set; } = new List<FileUploadDto>();
}