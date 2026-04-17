using Microsoft.EntityFrameworkCore;
using SmartCollab.Core.DTOs;
using SmartCollab.Core.Entities;
using SmartCollab.Core.Interfaces;
using SmartCollab.Infrastructure.Data;

namespace SmartCollab.Infrastructure.Services;

public class TaskService : ITaskService
{
    private readonly ApplicationDbContext _context;

    public TaskService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<TaskResponseDto>> GetWorkspaceTasksAsync(Guid workspaceId)
    {
        var tasks = await _context.Tasks
            .Where(t => t.WorkspaceId == workspaceId)
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .OrderByDescending(t => t.Priority)
            .Select(t => MapToTaskResponseDto(t))
            .ToListAsync();

        return tasks;
    }

    public async Task<TaskResponseDto?> CreateTaskAsync(Guid workspaceId, Guid userId, CreateTaskDto dto)
    {
        var task = new TaskItem
        {
            Title = dto.Title,
            Description = dto.Description,
            Priority = dto.Priority,
            WorkspaceId = workspaceId,
            CreatedById = userId,
            AssignedToId = dto.AssignedToId,
            DueDate = dto.DueDate?.ToUniversalTime(),
            Status = "Todo",
            CreatedAt = DateTime.UtcNow
        };

        _context.Tasks.Add(task);
        await _context.SaveChangesAsync();

        await _context.Entry(task)
            .Reference(t => t.AssignedTo)
            .LoadAsync();
        await _context.Entry(task)
            .Reference(t => t.CreatedBy)
            .LoadAsync();

        return MapToTaskResponseDto(task);
    }

    public async Task<TaskResponseDto?> UpdateTaskAsync(Guid taskId, Guid userId, UpdateTaskDto dto)
    {
        var task = await _context.Tasks
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return null;

        var isAdmin = await IsUserWorkspaceAdmin(task.WorkspaceId, userId);
        var isCreator = task.CreatedById == userId;

        if (!isAdmin && !isCreator)
            return null;

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Status = dto.Status;
        task.Priority = dto.Priority;
        task.AssignedToId = dto.AssignedToId;
        task.DueDate = dto.DueDate?.ToUniversalTime();
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToTaskResponseDto(task);
    }

    public async Task<TaskResponseDto?> UpdateTaskStatusAsync(Guid taskId, Guid userId, string status)
    {
        var task = await _context.Tasks
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null)
            return null;

        var isMember = await IsUserWorkspaceMember(task.WorkspaceId, userId);

        if (!isMember)
            return null;

        task.Status = status;
        task.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return MapToTaskResponseDto(task);
    }

    public async Task<bool> DeleteTaskAsync(Guid taskId, Guid userId)
    {
        var task = await _context.Tasks.FindAsync(taskId);

        if (task == null)
            return false;

        var isAdmin = await IsUserWorkspaceAdmin(task.WorkspaceId, userId);
        var isCreator = task.CreatedById == userId;

        if (!isAdmin && !isCreator)
            return false;

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();

        return true;
    }

    private async Task<bool> IsUserWorkspaceAdmin(Guid workspaceId, Guid userId)
    {
        var member = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(wm => wm.WorkspaceId == workspaceId && wm.UserId == userId);

        return member != null && member.Role == "Admin";
    }

    private async Task<bool> IsUserWorkspaceMember(Guid workspaceId, Guid userId)
    {
        return await _context.WorkspaceMembers
            .AnyAsync(wm => wm.WorkspaceId == workspaceId && wm.UserId == userId);
    }

    private TaskResponseDto MapToTaskResponseDto(TaskItem task)
    {
        var dueDate = task.DueDate;
        var isOverdue = dueDate.HasValue && dueDate.Value < DateTime.UtcNow && task.Status != "Done";

        return new TaskResponseDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            PriorityLabel = task.Priority == 1 ? "Low" : task.Priority == 2 ? "Medium" : "High",
            AssignedToName = task.AssignedTo != null ? $"{task.AssignedTo.FirstName} {task.AssignedTo.LastName}" : "Unassigned",
            AssignedToId = task.AssignedToId,
            CreatedByName = $"{task.CreatedBy.FirstName} {task.CreatedBy.LastName}",
            CreatedAt = task.CreatedAt,
            DueDate = task.DueDate,
            IsOverdue = isOverdue
        };
    }
}