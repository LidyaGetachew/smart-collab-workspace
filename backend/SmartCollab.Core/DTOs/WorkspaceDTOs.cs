using System.ComponentModel.DataAnnotations;

namespace SmartCollab.Core.DTOs;

public class CreateWorkspaceDto
{
    [Required]
    [MinLength(3)]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
}


public class UpdateWorkspaceDto
{
    [Required]
    [MinLength(3)]
    [MaxLength(200)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string Description { get; set; } = string.Empty;
}


public class WorkspaceResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string OwnerName { get; set; } = string.Empty;
    public Guid OwnerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
    public int TaskCount { get; set; }
    public string? OwnerEmail { get; set; }
}

public class InviteMemberDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Admin|Member)$", ErrorMessage = "Role must be either 'Admin' or 'Member'")]
    public string Role { get; set; } = "Member";
}

public class UpdateMemberRoleDto
{
    [Required]
    public Guid MemberId { get; set; }

    [Required]
    [RegularExpression("^(Admin|Member)$", ErrorMessage = "Role must be either 'Admin' or 'Member'")]
    public string Role { get; set; } = string.Empty;
}

public class WorkspaceMemberDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
    public string? AvatarUrl { get; set; }
}

public class RemoveMemberDto
{
    [Required]
    public Guid MemberId { get; set; }
}

public class WorkspaceSummaryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int TotalMembers { get; set; }
    public int TotalTasks { get; set; }
    public int CompletedTasks { get; set; }
    public int InProgressTasks { get; set; }
    public int TodoTasks { get; set; }
}

public class WorkspaceActivityDto
{
    public Guid Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string UserName { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}