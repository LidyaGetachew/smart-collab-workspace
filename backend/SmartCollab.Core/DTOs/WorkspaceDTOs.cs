using System.ComponentModel.DataAnnotations;

namespace SmartCollab.Core.DTOs;

public class CreateWorkspaceDto
{
    [Required]
    [MinLength(3)]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
    public string Description { get; set; } = string.Empty;
}

public class UpdateWorkspaceDto
{
    [Required]
    [MinLength(3)]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(500)]
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
    public int FileCount { get; set; }
}

public class InviteMemberDto
{
    [Required]
    [EmailAddress]
    public string Email { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Admin|Member)$")]
    public string Role { get; set; } = "Member";
}

public class WorkspaceMemberDto
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string UserEmail { get; set; } = string.Empty;
    public string? UserAvatar { get; set; }
    public string Role { get; set; } = string.Empty;
    public DateTime JoinedAt { get; set; }
}

public class UpdateMemberRoleDto
{
    [Required]
    public Guid MemberId { get; set; }

    [Required]
    [RegularExpression("^(Admin|Member)$")]
    public string Role { get; set; } = string.Empty;
}