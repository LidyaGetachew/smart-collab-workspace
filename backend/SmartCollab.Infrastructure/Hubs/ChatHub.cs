using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using SmartCollab.Core.Interfaces;
using System.Security.Claims;

namespace SmartCollab.Infrastructure.Hubs;

[Authorize]
public class ChatHub : Hub
{
    private readonly IChatService _chatService;
    private readonly IWorkspaceService _workspaceService;
    private readonly ILogger<ChatHub> _logger;

    public ChatHub(IChatService chatService, IWorkspaceService workspaceService, ILogger<ChatHub> logger)
    {
        _chatService = chatService;
        _workspaceService = workspaceService;
        _logger = logger;
    }

    public override async Task OnConnectedAsync()
    {
        _logger.LogInformation($"User connected: {Context.ConnectionId}");
        await base.OnConnectedAsync();
    }

    public async Task JoinWorkspaceRoom(Guid workspaceId)
    {
        var userId = GetUserId();

        if (await _workspaceService.IsUserInWorkspaceAsync(userId, workspaceId))
        {
            await Groups.AddToGroupAsync(Context.ConnectionId, $"workspace-{workspaceId}");
            _logger.LogInformation($"User {userId} joined workspace-{workspaceId}");

            // Send unread count
            var unreadCount = await _chatService.GetUnreadCountAsync(workspaceId, userId);
            await Clients.Caller.SendAsync("UnreadCount", unreadCount);
        }
    }

    public async Task LeaveWorkspaceRoom(Guid workspaceId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"workspace-{workspaceId}");
        _logger.LogInformation($"User left workspace-{workspaceId}");
    }

    public async Task SendMessage(Guid workspaceId, string message)
    {
        var userId = GetUserId();

        if (!await _workspaceService.IsUserInWorkspaceAsync(userId, workspaceId))
            return;

        var savedMessage = await _chatService.SaveMessageAsync(workspaceId, userId, message);

        await Clients.Group($"workspace-{workspaceId}").SendAsync("ReceiveMessage", savedMessage);

        // Notify about unread count
        var members = await _workspaceService.GetWorkspaceMembersAsync(workspaceId);
        foreach (var member in members.Where(m => m.UserId != userId))
        {
            var unreadCount = await _chatService.GetUnreadCountAsync(workspaceId, member.UserId);
            await Clients.User(member.UserId.ToString()).SendAsync("UnreadCountUpdated", workspaceId, unreadCount);
        }
    }

    public async Task LoadHistory(Guid workspaceId, int skip = 0, int take = 50)
    {
        var userId = GetUserId();

        if (!await _workspaceService.IsUserInWorkspaceAsync(userId, workspaceId))
            return;

        var messages = await _chatService.GetWorkspaceMessagesAsync(workspaceId, skip, take);
        await Clients.Caller.SendAsync("ChatHistory", messages);

        // Mark messages as read
        await _chatService.MarkMessagesAsReadAsync(workspaceId, userId);
    }

    public async Task Typing(Guid workspaceId, bool isTyping)
    {
        var userId = GetUserId();
        var user = await GetUserInfo(userId);

        await Clients.Group($"workspace-{workspaceId}").SendAsync("UserTyping", userId, user.UserName, isTyping);
    }

    private Guid GetUserId()
    {
        var claim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(claim, out var id) ? id : Guid.Empty;
    }

    private async Task<(Guid UserId, string UserName, string Avatar)> GetUserInfo(Guid userId)
    {
        // This would come from a user service
        return (userId, "User", null);
    }
}