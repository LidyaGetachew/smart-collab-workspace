namespace SmartCollab.Core.DTOs;

public class DashboardStatsDto
{
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int TodoTasks { get; set; }
    public double CompletionRate { get; set; }
    public int HighPriorityTasks { get; set; }
    public int OverdueTasks { get; set; }
    public int TotalMembers { get; set; }
    public int TotalFiles { get; set; }
    public int TotalMessages { get; set; }
}

public class TaskStatisticsDto
{
    public string Status { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Color { get; set; } = string.Empty;
    public double Percentage { get; set; }
}

public class RecentActivityDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public string? TaskTitle { get; set; }
    public DateTime Timestamp { get; set; }
    public string TimeAgo { get; set; } = string.Empty;
    public string Icon { get; set; } = string.Empty;
}

public class ActivityLogDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public DateTime CreatedAt { get; set; }
    public string TimeAgo { get; set; } = string.Empty;
}