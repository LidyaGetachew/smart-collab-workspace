using Microsoft.EntityFrameworkCore;
using SmartCollab.Core.DTOs;
using SmartCollab.Core.Entities;
using SmartCollab.Core.Interfaces;
using SmartCollab.Infrastructure.Data;

namespace SmartCollab.Infrastructure.Services;

public class TaskService : ITaskService
{
    private readonly ApplicationDbContext _context;
    private readonly IEventPublisher _eventPublisher;

    public TaskService(ApplicationDbContext context, IEventPublisher eventPublisher)
    {
        _context = context;
        _eventPublisher = eventPublisher;
    }

    public async Task<IEnumerable<TaskResponseDto>> GetWorkspaceTasksAsync(Guid workspaceId)
    {
        var tasks = await _context.Tasks
            .Where(t => t.WorkspaceId == workspaceId)
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .Include(t => t.Comments)
            .OrderByDescending(t => t.Priority)
            .ThenBy(t => t.CreatedAt)
            .Select(t => new TaskResponseDto
            {
                Id = t.Id,
                Title = t.Title,
                Description = t.Description,
                Status = t.Status,
                Priority = t.Priority,
                PriorityLabel = GetPriorityLabel(t.Priority),
                PriorityColor = GetPriorityColor(t.Priority),
                AssignedToName = t.AssignedTo != null ? $"{t.AssignedTo.FirstName} {t.AssignedTo.LastName}" : "Unassigned",
                AssignedToId = t.AssignedToId,
                AssignedToAvatar = t.AssignedTo != null ? t.AssignedTo.AvatarUrl : null,
                CreatedByName = t.CreatedBy != null ? $"{t.CreatedBy.FirstName} {t.CreatedBy.LastName}" : "Unknown",
                CreatedById = t.CreatedById,
                CreatedAt = t.CreatedAt,
                DueDate = t.DueDate,
                UpdatedAt = t.UpdatedAt,
                IsOverdue = t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow && t.Status != "Done",
                DaysUntilDue = t.DueDate.HasValue ? (int)(t.DueDate.Value - DateTime.UtcNow).TotalDays : 0,
                CommentCount = t.Comments.Count,
                StatusIcon = GetStatusIcon(t.Status)
            })
            .ToListAsync();

        return tasks;
    }

    public async Task<TaskResponseDto?> GetTaskByIdAsync(Guid taskId)
    {
        var task = await _context.Tasks
            .Include(t => t.AssignedTo)
            .Include(t => t.CreatedBy)
            .Include(t => t.Comments)
            .FirstOrDefaultAsync(t => t.Id == taskId);

        if (task == null) return null;

        return new TaskResponseDto
        {
            Id = task.Id,
            Title = task.Title,
            Description = task.Description,
            Status = task.Status,
            Priority = task.Priority,
            PriorityLabel = GetPriorityLabel(task.Priority),
            PriorityColor = GetPriorityColor(task.Priority),
            AssignedToName = task.AssignedTo != null ? $"{task.AssignedTo.FirstName} {task.AssignedTo.LastName}" : "Unassigned",
            AssignedToId = task.AssignedToId,
            CreatedByName = $"{task.CreatedBy.FirstName} {task.CreatedBy.LastName}",
            CreatedById = task.CreatedById,
            CreatedAt = task.CreatedAt,
            DueDate = task.DueDate,
            UpdatedAt = task.UpdatedAt,
            IsOverdue = task.DueDate.HasValue && task.DueDate.Value < DateTime.UtcNow && task.Status != "Done",
            DaysUntilDue = task.DueDate.HasValue ? (int)(task.DueDate.Value - DateTime.UtcNow).TotalDays : 0,
            CommentCount = task.Comments.Count
        };
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

        // Add activity log
        _context.ActivityLogs.Add(new ActivityLog
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            TaskId = task.Id,
            Action = "created",
            Description = $"Created task '{task.Title}'",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return await GetTaskByIdAsync(task.Id);
    }

    public async Task<TaskResponseDto?> UpdateTaskAsync(Guid taskId, Guid userId, UpdateTaskDto dto)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null) return null;

        var isAdmin = await IsUserWorkspaceAdmin(task.WorkspaceId, userId);
        var isCreator = task.CreatedById == userId;

        if (!isAdmin && !isCreator) return null;

        var oldStatus = task.Status;

        task.Title = dto.Title;
        task.Description = dto.Description;
        task.Status = dto.Status;
        task.Priority = dto.Priority;
        task.AssignedToId = dto.AssignedToId;
        task.DueDate = dto.DueDate?.ToUniversalTime();
        task.UpdatedAt = DateTime.UtcNow;

        if (task.Status == "Done" && oldStatus != "Done")
            task.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Add activity log
        _context.ActivityLogs.Add(new ActivityLog
        {
            WorkspaceId = task.WorkspaceId,
            UserId = userId,
            TaskId = taskId,
            Action = "updated",
            Description = $"Updated task '{task.Title}'",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return await GetTaskByIdAsync(taskId);
    }

    public async Task<TaskResponseDto?> UpdateTaskStatusAsync(Guid taskId, Guid userId, string status)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null) return null;

        var isMember = await IsUserWorkspaceMember(task.WorkspaceId, userId);
        if (!isMember) return null;

        var oldStatus = task.Status;
        task.Status = status;
        task.UpdatedAt = DateTime.UtcNow;

        if (status == "Done" && oldStatus != "Done")
            task.CompletedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Add activity log
        _context.ActivityLogs.Add(new ActivityLog
        {
            WorkspaceId = task.WorkspaceId,
            UserId = userId,
            TaskId = taskId,
            Action = "status_changed",
            Description = $"Changed status from {oldStatus} to {status}",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return await GetTaskByIdAsync(taskId);
    }

    public async Task<bool> DeleteTaskAsync(Guid taskId, Guid userId)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null) return false;

        var isAdmin = await IsUserWorkspaceAdmin(task.WorkspaceId, userId);
        var isCreator = task.CreatedById == userId;

        if (!isAdmin && !isCreator) return false;

        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<CommentResponseDto?> AddCommentAsync(Guid taskId, Guid userId, string content)
    {
        var task = await _context.Tasks.FindAsync(taskId);
        if (task == null) return null;

        var isMember = await IsUserWorkspaceMember(task.WorkspaceId, userId);
        if (!isMember) return null;

        var user = await _context.Users.FindAsync(userId);

        var comment = new Comment
        {
            Content = content,
            TaskId = taskId,
            AuthorId = userId,
            AuthorName = $"{user?.FirstName} {user?.LastName}",
            AuthorAvatar = user?.AvatarUrl,
            CreatedAt = DateTime.UtcNow
        };

        _context.Comments.Add(comment);
        await _context.SaveChangesAsync();

        // Add activity log
        _context.ActivityLogs.Add(new ActivityLog
        {
            WorkspaceId = task.WorkspaceId,
            UserId = userId,
            TaskId = taskId,
            Action = "commented",
            Description = $"Added comment: {(content.Length <= 50 ? content : content.Substring(0, 50) + "...")}",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return new CommentResponseDto
        {
            Id = comment.Id,
            Content = comment.Content,
            AuthorId = comment.AuthorId,
            AuthorName = comment.AuthorName,
            AuthorAvatar = comment.AuthorAvatar,
            CreatedAt = comment.CreatedAt,
            TimeAgo = GetTimeAgo(comment.CreatedAt)
        };
    }

    public async Task<IEnumerable<CommentResponseDto>> GetTaskCommentsAsync(Guid taskId)
    {
        return await _context.Comments
            .Where(c => c.TaskId == taskId)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new CommentResponseDto
            {
                Id = c.Id,
                Content = c.Content,
                AuthorId = c.AuthorId,
                AuthorName = c.AuthorName,
                AuthorAvatar = c.AuthorAvatar,
                CreatedAt = c.CreatedAt,
                TimeAgo = GetTimeAgo(c.CreatedAt)
            }).ToListAsync();
    }

    public async Task<bool> DeleteCommentAsync(Guid commentId, Guid userId)
    {
        var comment = await _context.Comments
            .Include(c => c.Task)
            .FirstOrDefaultAsync(c => c.Id == commentId);

        if (comment == null) return false;

        var isAdmin = await IsUserWorkspaceAdmin(comment.Task.WorkspaceId, userId);
        var isAuthor = comment.AuthorId == userId;

        if (!isAdmin && !isAuthor) return false;

        _context.Comments.Remove(comment);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<ActivityLogDto>> GetTaskActivitiesAsync(Guid taskId)
    {
        return await _context.ActivityLogs
            .Where(a => a.TaskId == taskId)
            .Include(a => a.User)
            .OrderByDescending(a => a.CreatedAt)
            .Select(a => new ActivityLogDto
            {
                Id = a.Id,
                Action = a.Action,
                Description = a.Description,
                UserName = $"{a.User.FirstName} {a.User.LastName}",
                UserAvatar = a.User.AvatarUrl,
                CreatedAt = a.CreatedAt,
                TimeAgo = GetTimeAgo(a.CreatedAt)
            }).ToListAsync();
    }

    public async Task<DashboardStatsDto> GetWorkspaceDashboardStatsAsync(Guid workspaceId)
    {
        var tasks = await _context.Tasks
            .Where(t => t.WorkspaceId == workspaceId)
            .ToListAsync();

        var members = await _context.WorkspaceMembers
            .CountAsync(wm => wm.WorkspaceId == workspaceId);

        var files = await _context.Files
            .CountAsync(f => f.WorkspaceId == workspaceId);

        var messages = await _context.ChatMessages
            .CountAsync(m => m.WorkspaceId == workspaceId);

        var completed = tasks.Count(t => t.Status == "Done");
        var inProgress = tasks.Count(t => t.Status == "InProgress");
        var todo = tasks.Count(t => t.Status == "Todo");
        var highPriority = tasks.Count(t => t.Priority == 3);
        var overdue = tasks.Count(t => t.DueDate.HasValue && t.DueDate.Value < DateTime.UtcNow && t.Status != "Done");

        return new DashboardStatsDto
        {
            TotalTasks = tasks.Count,
            CompletedTasks = completed,
            InProgressTasks = inProgress,
            TodoTasks = todo,
            CompletionRate = tasks.Count > 0 ? (double)completed / tasks.Count * 100 : 0,
            HighPriorityTasks = highPriority,
            OverdueTasks = overdue,
            TotalMembers = members,
            TotalFiles = files,
            TotalMessages = messages
        };
    }

    public async Task<IEnumerable<TaskStatisticsDto>> GetTaskStatisticsAsync(Guid workspaceId)
    {
        var total = await _context.Tasks.CountAsync(t => t.WorkspaceId == workspaceId);

        var stats = await _context.Tasks
            .Where(t => t.WorkspaceId == workspaceId)
            .GroupBy(t => t.Status)
            .Select(g => new TaskStatisticsDto
            {
                Status = g.Key,
                Label = GetStatusLabel(g.Key),
                Count = g.Count(),
                Color = GetStatusColor(g.Key),
                Percentage = total > 0 ? (double)g.Count() / total * 100 : 0
            }).ToListAsync();

        return stats;
    }

    public async Task<IEnumerable<RecentActivityDto>> GetRecentActivitiesAsync(Guid workspaceId, int limit)
    {
        var activities = await _context.ActivityLogs
            .Where(a => a.WorkspaceId == workspaceId)
            .Include(a => a.User)
            .Include(a => a.Task)
            .OrderByDescending(a => a.CreatedAt)
            .Take(limit)
            .Select(a => new RecentActivityDto
            {
                Id = a.Id,
                Action = a.Action,
                Description = a.Description,
                UserName = $"{a.User.FirstName} {a.User.LastName}",
                UserAvatar = a.User.AvatarUrl,
                TaskTitle = a.Task != null ? a.Task.Title : null,
                Timestamp = a.CreatedAt,
                TimeAgo = GetTimeAgo(a.CreatedAt),
                Icon = GetActivityIcon(a.Action)
            }).ToListAsync();

        return activities;
    }

    // Helper methods
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

    private static string GetPriorityLabel(int priority) => priority switch
    {
        1 => "Low",
        2 => "Medium",
        3 => "High",
        _ => "Medium"
    };

    private static string GetPriorityColor(int priority) => priority switch
    {
        1 => "success",
        2 => "warning",
        3 => "danger",
        _ => "info"
    };

    private static string GetStatusLabel(string status) => status switch
    {
        "Todo" => "To Do",
        "InProgress" => "In Progress",
        "Done" => "Done",
        _ => status
    };

    private static string GetStatusColor(string status) => status switch
    {
        "Todo" => "secondary",
        "InProgress" => "primary",
        "Done" => "success",
        _ => "default"
    };

    private static string GetStatusIcon(string status) => status switch
    {
        "Todo" => "📋",
        "InProgress" => "⚙️",
        "Done" => "✅",
        _ => "📌"
    };

    private static string GetActivityIcon(string action) => action switch
    {
        "created" => "➕",
        "updated" => "✏️",
        "commented" => "💬",
        "assigned" => "👤",
        "status_changed" => "🔄",
        "invited" => "📧",
        _ => "📌"
    };

    private static string GetTimeAgo(DateTime dateTime)
    {
        var diff = DateTime.UtcNow - dateTime;
        if (diff.TotalMinutes < 1) return "Just now";
        if (diff.TotalMinutes < 60) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalHours < 24) return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
        return dateTime.ToString("MMM dd");
    }
}