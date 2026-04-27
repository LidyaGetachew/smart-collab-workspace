using SmartCollab.Core.DTOs;

namespace SmartCollab.Core.Interfaces;

public interface ITaskService
{
    Task<IEnumerable<TaskResponseDto>> GetWorkspaceTasksAsync(Guid workspaceId);
    Task<TaskResponseDto?> GetTaskByIdAsync(Guid taskId);
    Task<TaskResponseDto?> CreateTaskAsync(Guid workspaceId, Guid userId, CreateTaskDto dto);
    Task<TaskResponseDto?> UpdateTaskAsync(Guid taskId, Guid userId, UpdateTaskDto dto);
    Task<TaskResponseDto?> UpdateTaskStatusAsync(Guid taskId, Guid userId, string status);
    Task<bool> DeleteTaskAsync(Guid taskId, Guid userId);
    Task<CommentResponseDto?> AddCommentAsync(Guid taskId, Guid userId, string content);
    Task<IEnumerable<CommentResponseDto>> GetTaskCommentsAsync(Guid taskId);
    Task<bool> DeleteCommentAsync(Guid commentId, Guid userId);
    Task<IEnumerable<ActivityLogDto>> GetTaskActivitiesAsync(Guid taskId);
    Task<DashboardStatsDto> GetWorkspaceDashboardStatsAsync(Guid workspaceId);
    Task<IEnumerable<TaskStatisticsDto>> GetTaskStatisticsAsync(Guid workspaceId);
    Task<IEnumerable<RecentActivityDto>> GetRecentActivitiesAsync(Guid workspaceId, int limit);
}