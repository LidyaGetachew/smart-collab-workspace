using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCollab.Core.Interfaces;

namespace SmartCollab.API.Controllers;

[Authorize]
[ApiController]
[Route("api/workspaces/{workspaceId}/[controller]")]
public class FilesController : ControllerBase
{
    private readonly IFileService _fileService;
    private readonly IWorkspaceService _workspaceService;
    private readonly IAuthService _authService;

    public FilesController(IFileService fileService, IWorkspaceService workspaceService, IAuthService authService)
    {
        _fileService = fileService;
        _workspaceService = workspaceService;
        _authService = authService;
    }

    [HttpGet]
    public async Task<IActionResult> GetFiles(Guid workspaceId)
    {
        var userId = _authService.GetCurrentUserId();
        if (!await _workspaceService.IsUserInWorkspaceAsync(userId, workspaceId))
            return Forbid();

        var files = await _fileService.GetWorkspaceFilesAsync(workspaceId);
        return Ok(files);
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadFile(Guid workspaceId, IFormFile file)
    {
        var userId = _authService.GetCurrentUserId();
        if (!await _workspaceService.IsUserInWorkspaceAsync(userId, workspaceId))
            return Forbid();

        var result = await _fileService.UploadFileAsync(workspaceId, userId, file);
        if (result == null) return BadRequest(new { message = "Upload failed" });
        return Ok(result);
    }

    [HttpGet("{fileId}/download")]
    public async Task<IActionResult> DownloadFile(Guid workspaceId, Guid fileId)
    {
        var userId = _authService.GetCurrentUserId();
        if (!await _workspaceService.IsUserInWorkspaceAsync(userId, workspaceId))
            return Forbid();

        var content = await _fileService.DownloadFileAsync(fileId, userId);
        if (content == null) return NotFound();

        var metadata = await _fileService.GetFileMetadataAsync(fileId);
        return File(content, "application/octet-stream", metadata?.FileName ?? "file");
    }

    [HttpDelete("{fileId}")]
    public async Task<IActionResult> DeleteFile(Guid workspaceId, Guid fileId)
    {
        var userId = _authService.GetCurrentUserId();
        if (!await _workspaceService.IsUserInWorkspaceAsync(userId, workspaceId))
            return Forbid();

        var result = await _fileService.DeleteFileAsync(fileId, userId);
        if (!result) return NotFound();
        return Ok(new { message = "File deleted" });
    }
}