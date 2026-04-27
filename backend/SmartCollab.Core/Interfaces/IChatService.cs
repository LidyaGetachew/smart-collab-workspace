using SmartCollab.Core.DTOs;

namespace SmartCollab.Core.Interfaces;

public interface IChatService
{
    Task<ChatMessageDto> SaveMessageAsync(Guid workspaceId, Guid userId, string message, string messageType = "text");
    Task<IEnumerable<ChatMessageDto>> GetWorkspaceMessagesAsync(Guid workspaceId, int skip = 0, int take = 50);
    Task<int> GetUnreadCountAsync(Guid workspaceId, Guid userId);
    Task MarkMessagesAsReadAsync(Guid workspaceId, Guid userId);
}