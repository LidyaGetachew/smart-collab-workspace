using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCollab.Core.Interfaces;

namespace SmartCollab.API.Controllers;

[Authorize]
[ApiController]
[Route("api/workspaces/{workspaceId}/[controller]")]
public class DashboardController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly IWorkspaceService _workspaceService;
    private readonly IAuthService _authService;

    public DashboardController(ITaskService taskService, IWorkspaceService workspaceService, IAuthService authService)
    {
        _taskService = taskService;
        _workspaceService = workspaceService;
        _authService = authService;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(Guid workspaceId)
    {
        var userId = _authService.GetCurrentUserId();
        if (!await _workspaceService.IsUserInWorkspaceAsync(userId, workspaceId))
            return Forbid();

        var stats = await _taskService.GetWorkspaceDashboardStatsAsync(workspaceId);
        return Ok(stats);
    }

    [HttpGet("task-statistics")]
    public async Task<IActionResult> GetTaskStatistics(Guid workspaceId)
    {
        var userId = _authService.GetCurrentUserId();
        if (!await _workspaceService.IsUserInWorkspaceAsync(userId, workspaceId))
            return Forbid();

        var statistics = await _taskService.GetTaskStatisticsAsync(workspaceId);
        return Ok(statistics);
    }

    [HttpGet("recent-activities")]
    public async Task<IActionResult> GetRecentActivities(Guid workspaceId, [FromQuery] int limit = 10)
    {
        var userId = _authService.GetCurrentUserId();
        if (!await _workspaceService.IsUserInWorkspaceAsync(userId, workspaceId))
            return Forbid();

        var activities = await _taskService.GetRecentActivitiesAsync(workspaceId, limit);
        return Ok(activities);
    }
}