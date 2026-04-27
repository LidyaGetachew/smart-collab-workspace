using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCollab.Core.DTOs;
using SmartCollab.Core.Interfaces;

namespace SmartCollab.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class WorkspacesController : ControllerBase
{
    private readonly IWorkspaceService _workspaceService;
    private readonly IAuthService _authService;

    public WorkspacesController(IWorkspaceService workspaceService, IAuthService authService)
    {
        _workspaceService = workspaceService;
        _authService = authService;
    }

    [HttpGet]
    public async Task<IActionResult> GetMyWorkspaces()
    {
        var userId = _authService.GetCurrentUserId();
        var workspaces = await _workspaceService.GetUserWorkspacesAsync(userId);
        return Ok(workspaces);
    }

    [HttpGet("{workspaceId}")]
    public async Task<IActionResult> GetWorkspace(Guid workspaceId)
    {
        var userId = _authService.GetCurrentUserId();
        if (!await _workspaceService.IsUserInWorkspaceAsync(userId, workspaceId))
            return Forbid();

        var workspace = await _workspaceService.GetWorkspaceByIdAsync(workspaceId);
        return Ok(workspace);
    }

    [HttpPost]
    public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceDto dto)
    {
        var userId = _authService.GetCurrentUserId();
        var workspace = await _workspaceService.CreateWorkspaceAsync(userId, dto);
        return Ok(workspace);
    }

    [HttpPut("{workspaceId}")]
    public async Task<IActionResult> UpdateWorkspace(Guid workspaceId, [FromBody] UpdateWorkspaceDto dto)
    {
        var userId = _authService.GetCurrentUserId();
        if (!await _workspaceService.IsUserWorkspaceAdminAsync(userId, workspaceId))
            return Forbid();

        var workspace = await _workspaceService.UpdateWorkspaceAsync(workspaceId, dto);
        return Ok(workspace);
    }

    [HttpDelete("{workspaceId}")]
    public async Task<IActionResult> DeleteWorkspace(Guid workspaceId)
    {
        var userId = _authService.GetCurrentUserId();
        var result = await _workspaceService.DeleteWorkspaceAsync(workspaceId, userId);
        if (!result) return Forbid();
        return Ok(new { message = "Workspace deleted" });
    }

    [HttpPost("{workspaceId}/invite")]
    public async Task<IActionResult> InviteMember(Guid workspaceId, [FromBody] InviteMemberDto dto)
    {
        var userId = _authService.GetCurrentUserId();
        var result = await _workspaceService.InviteMemberAsync(workspaceId, userId, dto.Email, dto.Role);
        if (!result.Success) return BadRequest(new { message = result.Message });
        return Ok(new { message = result.Message });
    }

    [HttpGet("{workspaceId}/members")]
    public async Task<IActionResult> GetMembers(Guid workspaceId)
    {
        var userId = _authService.GetCurrentUserId();
        if (!await _workspaceService.IsUserInWorkspaceAsync(userId, workspaceId))
            return Forbid();

        var members = await _workspaceService.GetWorkspaceMembersAsync(workspaceId);
        return Ok(members);
    }

    [HttpDelete("{workspaceId}/members/{memberId}")]
    public async Task<IActionResult> RemoveMember(Guid workspaceId, Guid memberId)
    {
        var userId = _authService.GetCurrentUserId();
        var result = await _workspaceService.RemoveMemberAsync(workspaceId, memberId, userId);
        if (!result) return BadRequest(new { message = "Failed to remove member" });
        return Ok(new { message = "Member removed" });
    }

    [HttpPut("{workspaceId}/members/{memberId}/role")]
    public async Task<IActionResult> UpdateMemberRole(Guid workspaceId, Guid memberId, [FromBody] UpdateMemberRoleDto dto)
    {
        var userId = _authService.GetCurrentUserId();
        var result = await _workspaceService.UpdateMemberRoleAsync(workspaceId, memberId, dto.Role, userId);
        if (!result) return BadRequest(new { message = "Failed to update role" });
        return Ok(new { message = "Role updated" });
    }
}