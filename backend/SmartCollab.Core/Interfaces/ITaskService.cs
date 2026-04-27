using SmartCollab.Core.DTOs;

namespace SmartCollab.Core.Interfaces;

public interface ITaskService
{
    Task<IEnumerable<TaskResponseDto>> GetWorkspaceTasksAsync(Guid workspaceId);
    Task<TaskResponseDto?> CreateTaskAsync(Guid workspaceId, Guid userId, CreateTaskDto dto);
    Task<TaskResponseDto?> UpdateTaskAsync(Guid taskId, Guid userId, UpdateTaskDto dto);
    Task<TaskResponseDto?> UpdateTaskStatusAsync(Guid taskId, Guid userId, string status);
    Task<bool> DeleteTaskAsync(Guid taskId, Guid userId);
    Task<CommentDto?> AddCommentAsync(Guid taskId, Guid userId, string content);
    Task<IEnumerable<CommentDto>> GetTaskCommentsAsync(Guid taskId);
}