using System.ComponentModel.DataAnnotations;

namespace SmartCollab.Core.DTOs;

public class CreateTaskDto
{
    [Required]
    [MinLength(3)]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Range(1, 3)]
    public int Priority { get; set; } = 2;

    public Guid? AssignedToId { get; set; }
    public DateTime? DueDate { get; set; }
}

public class UpdateTaskDto
{
    [Required]
    [MinLength(3)]
    [MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(2000)]
    public string Description { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Todo|InProgress|Done)$")]
    public string Status { get; set; } = string.Empty;

    [Range(1, 3)]
    public int Priority { get; set; }

    public Guid? AssignedToId { get; set; }
    public DateTime? DueDate { get; set; }
}

public class UpdateTaskStatusDto
{
    [Required]
    [RegularExpression("^(Todo|InProgress|Done)$")]
    public string Status { get; set; } = string.Empty;
}

public class TaskResponseDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int Priority { get; set; }
    public string PriorityLabel { get; set; } = string.Empty;
    public string PriorityColor { get; set; } = string.Empty;
    public string AssignedToName { get; set; } = string.Empty;
    public Guid? AssignedToId { get; set; }
    public string? AssignedToAvatar { get; set; }
    public string CreatedByName { get; set; } = string.Empty;
    public Guid CreatedById { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? DueDate { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public bool IsOverdue { get; set; }
    public int DaysUntilDue { get; set; }
    public int CommentCount { get; set; }
    public string? StatusIcon { get; set; }
}

public class BulkTaskUpdateDto
{
    public List<Guid> TaskIds { get; set; } = new();
    public string? Status { get; set; }
    public int? Priority { get; set; }
    public Guid? AssignedToId { get; set; }
}

public class TaskFilterDto
{
    public string? Status { get; set; }
    public int? Priority { get; set; }
    public Guid? AssignedToId { get; set; }
    public DateTime? DueDateFrom { get; set; }
    public DateTime? DueDateTo { get; set; }
    public string? SearchTerm { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}