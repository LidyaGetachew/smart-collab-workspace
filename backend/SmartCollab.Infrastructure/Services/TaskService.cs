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
        // Option 1: Use inline projection (MOST EFFICIENT)
        var tasks = await _context.Tasks
            .Where(t => t.WorkspaceId == workspaceId)
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .OrderByDescending(t => t.Priority)
            .Select(t => new TaskResponseDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                Priority = t.Priority,
                PriorityLabel = t.Priority == 1 ? "Low" : t.Priority == 2 ? "Medium" : "High",
                AssignedToName = t.AssignedTo != null ? t.AssignedTo.FirstName + " " + t.AssignedTo.LastName : "Unassigned",
                AssignedToId = t.AssignedToId,
                CreatedByName = t.CreatedBy != null ? t.CreatedBy.FirstName + " " + t.CreatedBy.LastName : "Unknown",
                CreatedById = t.CreatedById,
                CreatedAt = t.CreatedAt,
                DueDate = t.DueDate,
                UpdatedAt = t.UpdatedAt,
                IsOverdue = t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow && t.Status != "Done",
                DaysUntilDue = t.DueDate.HasValue ? (int)(t.DueDate.Value - DateTime.UtcNow).TotalDays : 0
            })
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

        // Load navigation properties
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

    public async Task<bool> BulkUpdateTasksAsync(Guid workspaceId, Guid userId, BulkTaskUpdateDto dto)
    {
        var isAdmin = await IsUserWorkspaceAdmin(workspaceId, userId);

        if (!isAdmin)
            return false;

        var tasks = await _context.Tasks
            .Where(t => dto.TaskIds.Contains(t.Id) && t.WorkspaceId == workspaceId)
            .ToListAsync();

        foreach (var task in tasks)
        {
            if (!string.IsNullOrEmpty(dto.Status))
                task.Status = dto.Status;

            if (dto.Priority.HasValue)
                task.Priority = dto.Priority.Value;

            if (dto.AssignedToId.HasValue)
                task.AssignedToId = dto.AssignedToId;

            task.UpdatedAt = DateTime.UtcNow;
        }

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<PaginatedTaskResponseDto> GetFilteredTasksAsync(Guid workspaceId, TaskFilterDto filter)
    {
        var query = _context.Tasks
            .Where(t => t.WorkspaceId == workspaceId)
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .AsQueryable();

        // Apply filters
        if (!string.IsNullOrEmpty(filter.Status))
            query = query.Where(t => t.Status == filter.Status);

        if (filter.Priority.HasValue)
            query = query.Where(t => t.Priority == filter.Priority.Value);

        if (filter.AssignedToId.HasValue)
            query = query.Where(t => t.AssignedToId == filter.AssignedToId.Value);

        if (filter.DueDateFrom.HasValue)
            query = query.Where(t => t.DueDate >= filter.DueDateFrom.Value);

        if (filter.DueDateTo.HasValue)
            query = query.Where(t => t.DueDate <= filter.DueDateTo.Value);

        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            query = query.Where(t =>
                t.Title.Contains(filter.SearchTerm) ||
                t.Description.Contains(filter.SearchTerm));
        }

        var totalCount = await query.CountAsync();

        var tasks = await query
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .Select(t => new TaskResponseDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                Priority = t.Priority,
                PriorityLabel = t.Priority == 1 ? "Low" : t.Priority == 2 ? "Medium" : "High",
                AssignedToName = t.AssignedTo != null ? t.AssignedTo.FirstName + " " + t.AssignedTo.LastName : "Unassigned",
                AssignedToId = t.AssignedToId,
                CreatedByName = t.CreatedBy != null ? t.CreatedBy.FirstName + " " + t.CreatedBy.LastName : "Unknown",
                CreatedById = t.CreatedById,
                CreatedAt = t.CreatedAt,
                DueDate = t.DueDate,
                UpdatedAt = t.UpdatedAt,
                IsOverdue = t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow && t.Status != "Done",
                DaysUntilDue = t.DueDate.HasValue ? (int)(t.DueDate.Value - DateTime.UtcNow).TotalDays : 0
            })
            .ToListAsync();

        return new PaginatedTaskResponseDto
        {
            Tasks = tasks,
            TotalCount = totalCount,
            Page = filter.Page,
            PageSize = filter.PageSize,
            TotalPages = (int)Math.Ceiling(totalCount / (double)filter.PageSize)
        };
    }

    public async Task<IEnumerable<TaskResponseDto>> GetUserTasksAsync(Guid userId)
    {
        var tasks = await _context.Tasks
            .Where(t => t.AssignedToId == userId)
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .Include(t => t.Workspace)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.DueDate)
            .Select(t => new TaskResponseDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                Priority = t.Priority,
                PriorityLabel = t.Priority == 1 ? "Low" : t.Priority == 2 ? "Medium" : "High",
                AssignedToName = t.AssignedTo != null ? t.AssignedTo.FirstName + " " + t.AssignedTo.LastName : "Unassigned",
                AssignedToId = t.AssignedToId,
                CreatedByName = t.CreatedBy != null ? t.CreatedBy.FirstName + " " + t.CreatedBy.LastName : "Unknown",
                CreatedById = t.CreatedById,
                CreatedAt = t.CreatedAt,
                DueDate = t.DueDate,
                UpdatedAt = t.UpdatedAt,
                IsOverdue = t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow && t.Status != "Done",
                DaysUntilDue = t.DueDate.HasValue ? (int)(t.DueDate.Value - DateTime.UtcNow).TotalDays : 0
            })
            .ToListAsync();

        return tasks;
    }

    public async Task<CommentDto?> AddCommentAsync(Guid taskId, Guid userId, string content)
    {
        var task = await _context.Tasks.Include(t => t.Workspace).FirstOrDefaultAsync(t => t.Id == taskId);
        if (task == null) return null;

        var user = await _context.Users.FindAsync(userId);
        var comment = new Comment
        {
            Content = content,
            TaskId = taskId,
            AuthorId = userId,
            AuthorName = $"{user?.FirstName} {user?.LastName}"
        };
        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        return new CommentDto
        {
            Id = comment.Id,
            Content = comment.Content,
            AuthorId = comment.AuthorId,
            AuthorName = comment.AuthorName,
            CreatedAt = comment.CreatedAt
        };
    }

    public async Task<IEnumerable<CommentDto>> GetTaskCommentsAsync(Guid taskId)
    {
        return await _context.Comments
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentDto
            {
                Id = c.Id,
                Content = c.Content,
                AuthorId = c.AuthorId,
                AuthorName = c.AuthorName,
                CreatedAt = c.CreatedAt
            }).ToListAsync();
    }
    // Private helper methods
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

    // Make this static - KEY FIX
    private static TaskResponseDto MapToTaskResponseDto(TaskItem task)
    {
        var dueDate = task.DueDate;
        var isOverdue = dueDate.HasValue && dueDate.Value < DateTime.UtcNow && task.Status != "Done";
        var daysUntilDue = dueDate.HasValue ? (int)(dueDate.Value - DateTime.UtcNow).TotalDays : 0;

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
            CreatedByName = task.CreatedBy != null ? $"{task.CreatedBy.FirstName} {task.CreatedBy.LastName}" : "Unknown",
            CreatedById = task.CreatedById,
            CreatedAt = task.CreatedAt,
            DueDate = task.DueDate,
            UpdatedAt = task.UpdatedAt,
            IsOverdue = isOverdue,
            DaysUntilDue = daysUntilDue
        };
    }
}