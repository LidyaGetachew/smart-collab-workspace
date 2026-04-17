using Microsoft.AspNetCore.Http;
using SmartCollab.Core.DTOs;

namespace SmartCollab.Core.Interfaces;

public interface IFileService
{
    Task<FileResponseDto?> UploadFileAsync(Guid workspaceId, Guid userId, IFormFile file);
    Task<IEnumerable<FileResponseDto>> GetWorkspaceFilesAsync(Guid workspaceId);
    Task<FileDownloadDto?> DownloadFileAsync(Guid fileId, Guid userId);
    Task<bool> DeleteFileAsync(Guid fileId, Guid userId);
}