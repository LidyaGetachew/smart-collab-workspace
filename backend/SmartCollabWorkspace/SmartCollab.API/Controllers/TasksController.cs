using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SmartCollab.Core.DTOs;
using SmartCollab.Core.Interfaces;

namespace SmartCollab.API.Controllers;

[Authorize]
[ApiController]
[Route("api/workspaces/{workspaceId}/[controller]")]
public class TasksController : ControllerBase
{
    private readonly ITaskService _taskService;
    private readonly IWorkspaceService _workspaceService;
    private readonly IAuthService _authService;

    public TasksController(ITaskService taskService, IWorkspaceService workspaceService, IAuthService authService)
    {
        _taskService = taskService;
        _workspaceService = workspaceService;
        _authService = authService;
    }

    [HttpGet]
    public async Task<IActionResult> GetTasks(Guid workspaceId)
    {
        var userId = _authService.GetCurrentUserId();

        if (!await _workspaceService.IsUserInWorkspaceAsync(userId, workspaceId))
            return Forbid();

        var tasks = await _taskService.GetWorkspaceTasksAsync(workspaceId);
        return Ok(tasks);
    }

    [HttpPost]
    public async Task<IActionResult> CreateTask(Guid workspaceId, [FromBody] CreateTaskDto dto)
    {
        var userId = _authService.GetCurrentUserId();

        if (!await _workspaceService.IsUserInWorkspaceAsync(userId, workspaceId))
            return Forbid();

        var task = await _taskService.CreateTaskAsync(workspaceId, userId, dto);
        return Ok(task);
    }

    [HttpPut("{taskId}")]
    public async Task<IActionResult> UpdateTask(Guid workspaceId, Guid taskId, [FromBody] UpdateTaskDto dto)
    {
        var userId = _authService.GetCurrentUserId();

        if (!await _workspaceService.IsUserInWorkspaceAsync(userId, workspaceId))
            return Forbid();

        var task = await _taskService.UpdateTaskAsync(taskId, userId, dto);

        if (task == null)
            return NotFound(new { message = "Task not found" });

        return Ok(task);
    }

    [HttpPatch("{taskId}/status")]
    public async Task<IActionResult> UpdateTaskStatus(Guid workspaceId, Guid taskId, [FromBody] string status)
    {
        var userId = _authService.GetCurrentUserId();

        if (!await _workspaceService.IsUserInWorkspaceAsync(userId, workspaceId))
            return Forbid();

        var task = await _taskService.UpdateTaskStatusAsync(taskId, userId, status);

        if (task == null)
            return NotFound(new { message = "Task not found" });

        return Ok(task);
    }

    [HttpDelete("{taskId}")]
    public async Task<IActionResult> DeleteTask(Guid workspaceId, Guid taskId)
    {
        var userId = _authService.GetCurrentUserId();

        if (!await _workspaceService.IsUserInWorkspaceAsync(userId, workspaceId))
            return Forbid();

        var result = await _taskService.DeleteTaskAsync(taskId, userId);

        if (!result)
            return NotFound(new { message = "Task not found" });

        return Ok(new { message = "Task deleted successfully" });
    }

    [HttpPost("{taskId}/comments")]
    public async Task<IActionResult> AddComment(Guid workspaceId, Guid taskId, [FromBody] string content)
    {
        var userId = _authService.GetCurrentUserId();
        if (!await _workspaceService.IsUserInWorkspaceAsync(userId, workspaceId)) return Forbid();
        return Ok(await _taskService.AddCommentAsync(taskId, userId, content));
    }

    [HttpGet("{taskId}/comments")]
    public async Task<IActionResult> GetComments(Guid workspaceId, Guid taskId)
    {
        var userId = _authService.GetCurrentUserId();
        if (!await _workspaceService.IsUserInWorkspaceAsync(userId, workspaceId)) return Forbid();
        return Ok(await _taskService.GetTaskCommentsAsync(taskId));
    }
}