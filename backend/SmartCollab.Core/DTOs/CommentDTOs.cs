using System.ComponentModel.DataAnnotations;

namespace SmartCollab.Core.DTOs;

public class CreateCommentDto
{
    [Required]
    [MinLength(1)]
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;
}

public class UpdateCommentDto
{
    [Required]
    [MinLength(1)]
    [MaxLength(1000)]
    public string Content { get; set; } = string.Empty;
}

public class CommentResponseDto
{
    public Guid Id { get; set; }
    public string Content { get; set; } = string.Empty;
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? AuthorAvatar { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public string TimeAgo { get; set; } = string.Empty;
}