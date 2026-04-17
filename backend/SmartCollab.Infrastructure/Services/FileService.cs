using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
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
    private readonly IWebHostEnvironment _environment;
    private readonly IConfiguration _configuration;

    public FileService(
        ApplicationDbContext context,
        IWebHostEnvironment environment,
        IConfiguration configuration)
    {
        _context = context;
        _environment = environment;
        _configuration = configuration;
    }

    public async Task<FileResponseDto?> UploadFileAsync(Guid workspaceId, Guid userId, IFormFile file)
    {
        if (file == null || file.Length == 0)
            return null;

        var maxSizeMB = _configuration.GetValue<int>("FileUpload:MaxSizeInMB", 10);
        if (file.Length > maxSizeMB * 1024 * 1024)
            return null;

        var uploadsFolder = Path.Combine(_environment.WebRootPath ?? "uploads", workspaceId.ToString());
        if (!Directory.Exists(uploadsFolder))
            Directory.CreateDirectory(uploadsFolder);

        var uniqueFileName = $"{Guid.NewGuid()}_{file.FileName}";
        var filePath = Path.Combine(uploadsFolder, uniqueFileName);

        using (var stream = new FileStream(filePath, FileMode.Create))
        {
            await file.CopyToAsync(stream);
        }

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

        await _context.Entry(fileEntity)
            .Reference(f => f.UploadedBy)
            .LoadAsync();

        return MapToFileResponseDto(fileEntity);
    }

    public async Task<IEnumerable<FileResponseDto>> GetWorkspaceFilesAsync(Guid workspaceId)
    {
        var files = await _context.Files
            .Where(f => f.WorkspaceId == workspaceId)
            .Include(f => f.UploadedBy)
            .OrderByDescending(f => f.UploadedAt)
            .Select(f => MapToFileResponseDto(f))
            .ToListAsync();

        return files;
    }

    public async Task<FileDownloadDto?> DownloadFileAsync(Guid fileId, Guid userId)
    {
        var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == fileId);
        if (file == null)
            return null;

        var hasAccess = await _context.WorkspaceMembers
            .AnyAsync(wm => wm.WorkspaceId == file.WorkspaceId && wm.UserId == userId);

        if (!hasAccess || !File.Exists(file.FilePath))
            return null;

        var content = await File.ReadAllBytesAsync(file.FilePath);

        return new FileDownloadDto
        {
            Content = content,
            ContentType = file.MimeType,
            FileName = file.FileName
        };
    }

    public async Task<bool> DeleteFileAsync(Guid fileId, Guid userId)
    {
        var file = await _context.Files.FirstOrDefaultAsync(f => f.Id == fileId);

        if (file == null)
            return false;

        var isAdmin = await IsUserWorkspaceAdmin(file.WorkspaceId, userId);
        var isUploader = file.UploadedById == userId;

        if (!isAdmin && !isUploader)
            return false;

        if (File.Exists(file.FilePath))
            File.Delete(file.FilePath);

        _context.Files.Remove(file);
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<bool> IsUserWorkspaceAdmin(Guid workspaceId, Guid userId)
    {
        var member = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(wm => wm.WorkspaceId == workspaceId && wm.UserId == userId);

        return member != null && member.Role == "Admin";
    }

    private FileResponseDto MapToFileResponseDto(FileEntity file)
    {
        string[] sizes = { "B", "KB", "MB", "GB" };
        double len = file.FileSize;
        int order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len = len / 1024;
        }
        var formattedSize = $"{len:0.##} {sizes[order]}";

        return new FileResponseDto
        {
            Id = file.Id,
            FileName = file.FileName,
            FileSize = file.FileSize,
            FormattedFileSize = formattedSize,
            MimeType = file.MimeType,
            UploadedByName = $"{file.UploadedBy.FirstName} {file.UploadedBy.LastName}",
            UploadedAt = file.UploadedAt
        };
    }
}