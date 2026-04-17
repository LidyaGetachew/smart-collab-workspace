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

    [HttpPost]
    public async Task<IActionResult> CreateWorkspace([FromBody] CreateWorkspaceDto dto)
    {
        var userId = _authService.GetCurrentUserId();
        var workspace = await _workspaceService.CreateWorkspaceAsync(userId, dto);
        return Ok(workspace);
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
        var members = await _workspaceService.GetWorkspaceMembersAsync(workspaceId);
        return Ok(members);
    }
}