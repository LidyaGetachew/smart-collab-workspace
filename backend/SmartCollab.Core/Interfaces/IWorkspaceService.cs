using SmartCollab.Core.DTOs;
using SmartCollab.Core.Entities;

namespace SmartCollab.Core.Interfaces;

public interface IWorkspaceService
{
    Task<IEnumerable<WorkspaceResponseDto>> GetUserWorkspacesAsync(Guid userId);
    Task<WorkspaceResponseDto?> CreateWorkspaceAsync(Guid userId, CreateWorkspaceDto dto);
    Task<bool> InviteMemberAsync(Guid workspaceId, Guid invitedByUserId, string targetEmail, string role);
    Task<bool> IsUserInWorkspaceAsync(Guid userId, Guid workspaceId);
    Task<IEnumerable<WorkspaceMemberDto>> GetWorkspaceMembersAsync(Guid workspaceId);
}