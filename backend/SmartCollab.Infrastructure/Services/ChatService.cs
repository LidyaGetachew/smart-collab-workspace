using Microsoft.EntityFrameworkCore;
using SmartCollab.Core.DTOs;
using SmartCollab.Core.Entities;
using SmartCollab.Core.Interfaces;
using SmartCollab.Infrastructure.Data;

namespace SmartCollab.Infrastructure.Services;

public class ChatService : IChatService
{
    private readonly ApplicationDbContext _context;

    public ChatService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ChatMessageDto> SaveMessageAsync(Guid workspaceId, Guid userId, string message, string messageType = "text")
    {
        var user = await _context.Users.FindAsync(userId);

        var chatMessage = new ChatMessage
        {
            WorkspaceId = workspaceId,
            UserId = userId,
            UserName = $"{user?.FirstName} {user?.LastName}",
            UserEmail = user?.Email ?? "",
            UserAvatar = user?.AvatarUrl,
            Message = message,
            MessageType = messageType,
            SentAt = DateTime.UtcNow,
            IsRead = false
        };

        _context.ChatMessages.Add(chatMessage);
        await _context.SaveChangesAsync();

        return MapToChatMessageDto(chatMessage);
    }

    public async Task<IEnumerable<ChatMessageDto>> GetWorkspaceMessagesAsync(Guid workspaceId, int skip = 0, int take = 50)
    {
        var messages = await _context.ChatMessages
            .Where(m => m.WorkspaceId == workspaceId)
            .OrderByDescending(m => m.SentAt)
            .Skip(skip)
            .Take(take)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

        return messages.Select(MapToChatMessageDto);
    }

    public async Task<int> GetUnreadCountAsync(Guid workspaceId, Guid userId)
    {
        return await _context.ChatMessages
            .CountAsync(m => m.WorkspaceId == workspaceId && m.UserId != userId && !m.IsRead);
    }

    public async Task MarkMessagesAsReadAsync(Guid workspaceId, Guid userId)
    {
        var unreadMessages = await _context.ChatMessages
            .Where(m => m.WorkspaceId == workspaceId && m.UserId != userId && !m.IsRead)
            .ToListAsync();

        foreach (var message in unreadMessages)
        {
            message.IsRead = true;
        }

        await _context.SaveChangesAsync();
    }

    private static ChatMessageDto MapToChatMessageDto(ChatMessage message)
    {
        return new ChatMessageDto
        {
            Id = message.Id,
            WorkspaceId = message.WorkspaceId,
            UserId = message.UserId,
            UserName = message.UserName,
            UserEmail = message.UserEmail,
            UserAvatar = message.UserAvatar,
            Message = message.Message,
            MessageType = message.MessageType,
            SentAt = message.SentAt,
            TimeAgo = GetTimeAgo(message.SentAt)
        };
    }

    private static string GetTimeAgo(DateTime dateTime)
    {
        var diff = DateTime.UtcNow - dateTime;
        if (diff.TotalMinutes < 1) return "Just now";
        if (diff.TotalHours < 1) return $"{(int)diff.TotalMinutes}m ago";
        if (diff.TotalDays < 1) return $"{(int)diff.TotalHours}h ago";
        if (diff.TotalDays < 7) return $"{(int)diff.TotalDays}d ago";
        return dateTime.ToString("MMM dd");
    }
}