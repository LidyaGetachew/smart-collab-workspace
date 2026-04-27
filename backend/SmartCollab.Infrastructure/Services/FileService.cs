using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartCollab.Core.DTOs;
using SmartCollab.Core.Entities;
using SmartCollab.Core.Interfaces;
using SmartCollab.Infrastructure.Data;

namespace SmartCollab.Infrastructure.Services;

public class FileService : IFileService
{
    private readonly ApplicationDbContext _context;
    private readonly IWebHostEnvironment _env;
    private readonly IConfiguration _config;

    public FileService(ApplicationDbContext context, IWebHostEnvironment env, IConfiguration config)
    {
        _context = context;
        _env = env;
        _config = config;
    }

    public async Task<FileResponseDto?> UploadFileAsync(Guid workspaceId, Guid userId, IFormFile file)
    {
        if (file == null || file.Length == 0) return null;

        var maxSizeMB = _config.GetValue<int>("FileUpload:MaxSizeInMB", 10);
        if (file.Length > maxSizeMB * 1024 * 1024) return null;

        var uploadDir = Path.Combine(_env.WebRootPath ?? "uploads", workspaceId.ToString());
        if (!Directory.Exists(uploadDir)) Directory.CreateDirectory(uploadDir);

        var fileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(uploadDir, fileName);

        await using (var stream = new FileStream(filePath, FileMode.Create))
            await file.CopyToAsync(stream);

        var fileEntity = new FileEntity
        {
            FileName = file.FileName,
            FilePath = filePath,
            FileSize = file.Length,
            MimeType = file.ContentType,
            WorkspaceId = workspaceId,
            UploadedById = userId,
            UploadedAt = DateTime.UtcNow
        };

        _context.Files.Add(fileEntity);
        await _context.SaveChangesAsync();

        await _context.Entry(fileEntity).Reference(f => f.UploadedBy).LoadAsync();

        return MapToFileResponseDto(fileEntity);
    }

    public async Task<IEnumerable<FileResponseDto>> GetWorkspaceFilesAsync(Guid workspaceId)
    {
        var files = await _context.Files
            .Where(f => f.WorkspaceId == workspaceId)
            .Include(f => f.UploadedBy)
            .OrderByDescending(f => f.UploadedAt)
            .ToListAsync();

        return files.Select(MapToFileResponseDto);
    }

    public async Task<byte[]?> DownloadFileAsync(Guid fileId, Guid userId)
    {
        var file = await _context.Files.FindAsync(fileId);
        if (file == null) return null;

        var hasAccess = await _context.WorkspaceMembers
            .AnyAsync(wm => wm.WorkspaceId == file.WorkspaceId && wm.UserId == userId);

        if (!hasAccess || !File.Exists(file.FilePath)) return null;

        return await File.ReadAllBytesAsync(file.FilePath);
    }

    public async Task<bool> DeleteFileAsync(Guid fileId, Guid userId)
    {
        var file = await _context.Files.FindAsync(fileId);
        if (file == null) return false;

        var isAdmin = await IsUserWorkspaceAdmin(file.WorkspaceId, userId);
        var isUploader = file.UploadedById == userId;

        if (!isAdmin && !isUploader) return false;

        if (File.Exists(file.FilePath))
            File.Delete(file.FilePath);

        _context.Files.Remove(file);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<FileResponseDto?> GetFileMetadataAsync(Guid fileId)
    {
        var file = await _context.Files
            .Include(f => f.UploadedBy)
            .FirstOrDefaultAsync(f => f.Id == fileId);

        return file != null ? MapToFileResponseDto(file) : null;
    }

    public async Task<long> GetWorkspaceTotalStorageAsync(Guid workspaceId)
    {
        return await _context.Files
            .Where(f => f.WorkspaceId == workspaceId)
            .SumAsync(f => f.FileSize);
    }

    private async Task<bool> IsUserWorkspaceAdmin(Guid workspaceId, Guid userId)
    {
        var member = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(wm => wm.WorkspaceId == workspaceId && wm.UserId == userId);
        return member != null && member.Role == "Admin";
    }

    private static FileResponseDto MapToFileResponseDto(FileEntity file)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = file.FileSize;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }

        var timeAgo = GetTimeAgo(file.UploadedAt);
        var icon = GetFileIcon(file.MimeType);

        return new FileResponseDto
        {
            Id = file.Id,
            FileName = file.FileName,
            FileSize = file.FileSize,
            FormattedFileSize = $"{len:0.##} {sizes[order]}",
            MimeType = file.MimeType,
            FileIcon = icon,
            UploadedByName = file.UploadedBy != null ? $"{file.UploadedBy.FirstName} {file.UploadedBy.LastName}" : "Unknown",
            UploadedById = file.UploadedById,
            UploadedAt = file.UploadedAt,
            UploadedAtRelative = timeAgo
        };
    }

    private static string GetFileIcon(string mimeType)
    {
        if (mimeType.StartsWith("image/")) return "🖼️";
        if (mimeType.StartsWith("video/")) return "🎥";
        if (mimeType.StartsWith("audio/")) return "🎵";
        if (mimeType.Contains("pdf")) return "📄";
        if (mimeType.Contains("word")) return "📝";
        if (mimeType.Contains("excel")) return "📊";
        if (mimeType.Contains("powerpoint")) return "📽️";
        if (mimeType.Contains("zip")) return "🗜️";
        return "📁";
    }

    private static string GetTimeAgo(DateTime dateTime)
    {
        var diff = DateTime.UtcNow - dateTime;
        if (diff.TotalMinutes < 1) return "Just now";
        if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalDays < 1) return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
        return dateTime.ToString("MMM dd");
    }
}