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
    public async Task<IActionResult> GetWorkspaceById(Guid workspaceId)
    {
        var userId = _authService.GetCurrentUserId();

        // Check if user has access to this workspace
        if (!await _workspaceService.IsUserInWorkspaceAsync(userId, workspaceId))
            return Forbid();

        var workspace = await _workspaceService.GetWorkspaceByIdAsync(workspaceId);
        if (workspace == null)
            return NotFound(new { message = "Workspace not found" });

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

        // Check if user is admin
        if (!await _workspaceService.IsUserWorkspaceAdminAsync(userId, workspaceId))
            return Forbid();

        var workspace = await _workspaceService.UpdateWorkspaceAsync(workspaceId, dto);
        if (workspace == null)
            return NotFound(new { message = "Workspace not found" });

        return Ok(workspace);
    }

    [HttpDelete("{workspaceId}")]
    public async Task<IActionResult> DeleteWorkspace(Guid workspaceId)
    {
        var userId = _authService.GetCurrentUserId();

        // Check if user is owner or admin
        if (!await _workspaceService.IsUserWorkspaceAdminAsync(userId, workspaceId))
            return Forbid();

        var result = await _workspaceService.DeleteWorkspaceAsync(workspaceId, userId);
        if (!result)
            return BadRequest(new { message = "Failed to delete workspace" });

        return Ok(new { message = "Workspace deleted successfully" });
    }

    [HttpPost("{workspaceId}/invite")]
    public async Task<IActionResult> InviteMember(Guid workspaceId, [FromBody] InviteMemberDto dto)
    {
        var userId = _authService.GetCurrentUserId();
        var result = await _workspaceService.InviteMemberAsync(workspaceId, userId, dto.Email, dto.Role);

        if (!result)
            return BadRequest(new { message = "Failed to invite member" });

        return Ok(new { message = "Member invited successfully" });
    }

    [HttpGet("{workspaceId}/members")]
    public async Task<IActionResult> GetMembers(Guid workspaceId)
    {
        var userId = _authService.GetCurrentUserId();

        // Check if user has access to this workspace
        if (!await _workspaceService.IsUserInWorkspaceAsync(userId, workspaceId))
            return Forbid();

        var members = await _workspaceService.GetWorkspaceMembersAsync(workspaceId);
        return Ok(members);
    }

    // NEW: Remove member from workspace
    [HttpDelete("{workspaceId}/members/{memberId}")]
    public async Task<IActionResult> RemoveMember(Guid workspaceId, Guid memberId)
    {
        var userId = _authService.GetCurrentUserId();

        // Check if current user is admin
        if (!await _workspaceService.IsUserWorkspaceAdminAsync(userId, workspaceId))
            return Forbid();

        // Prevent removing yourself
        var memberToRemove = await _workspaceService.GetWorkspaceMemberByIdAsync(memberId);
        if (memberToRemove?.UserId == userId)
            return BadRequest(new { message = "You cannot remove yourself. Transfer ownership or delete the workspace." });

        var result = await _workspaceService.RemoveMemberFromWorkspaceAsync(workspaceId, memberId);
        if (!result)
            return NotFound(new { message = "Member not found" });

        return Ok(new { message = "Member removed successfully" });
    }

    // NEW: Update member role
    [HttpPut("{workspaceId}/members/{memberId}/role")]
    public async Task<IActionResult> UpdateMemberRole(Guid workspaceId, Guid memberId, [FromBody] UpdateMemberRoleDto dto)
    {
        var userId = _authService.GetCurrentUserId();

        // Check if current user is admin
        if (!await _workspaceService.IsUserWorkspaceAdminAsync(userId, workspaceId))
            return Forbid();

        var result = await _workspaceService.UpdateMemberRoleAsync(workspaceId, memberId, dto.Role);
        if (!result)
            return NotFound(new { message = "Member not found" });

        return Ok(new { message = "Member role updated successfully" });
    }

    // NEW: Leave workspace
    [HttpPost("{workspaceId}/leave")]
    public async Task<IActionResult> LeaveWorkspace(Guid workspaceId)
    {
        var userId = _authService.GetCurrentUserId();

        // Check if user is the owner
        var isOwner = await _workspaceService.IsUserWorkspaceOwnerAsync(userId, workspaceId);
        if (isOwner)
            return BadRequest(new { message = "Owner cannot leave workspace. Transfer ownership or delete the workspace." });

        var result = await _workspaceService.RemoveMemberFromWorkspaceByUserIdAsync(workspaceId, userId);
        if (!result)
            return NotFound(new { message = "You are not a member of this workspace" });

        return Ok(new { message = "You have left the workspace" });
    }
}