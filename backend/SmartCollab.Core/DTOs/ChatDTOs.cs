namespace SmartCollab.Core.DTOs;

public class ChatMessageDto
{
    public Guid Id { get; set; }
    public Guid WorkspaceId { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public string Message { get; set; } = string.Empty;
    public string MessageType { get; set; } = "text";
    public DateTime SentAt { get; set; }
    public string TimeAgo { get; set; } = string.Empty;
}

public class SendMessageDto
{
    public string Message { get; set; } = string.Empty;
    public string? MessageType { get; set; } = "text";
}

public class ChatHistoryResponseDto
{
    public List<ChatMessageDto> Messages { get; set; } = new();
    public int TotalCount { get; set; }
    public bool HasMore { get; set; }
}