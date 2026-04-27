using SmartCollab.Core.DTOs;

namespace SmartCollab.Core.Interfaces;

public interface IWorkspaceService
{
    Task<IEnumerable<WorkspaceResponseDto>> GetUserWorkspacesAsync(Guid userId);
    Task<WorkspaceResponseDto?> GetWorkspaceByIdAsync(Guid workspaceId);
    Task<WorkspaceResponseDto?> CreateWorkspaceAsync(Guid userId, CreateWorkspaceDto dto);
    Task<WorkspaceResponseDto?> UpdateWorkspaceAsync(Guid workspaceId, UpdateWorkspaceDto dto);
    Task<bool> DeleteWorkspaceAsync(Guid workspaceId, Guid userId);
    Task<(bool Success, string Message)> InviteMemberAsync(Guid workspaceId, Guid invitedByUserId, string targetEmail, string role);
    Task<bool> RemoveMemberAsync(Guid workspaceId, Guid memberId, Guid currentUserId);
    Task<bool> UpdateMemberRoleAsync(Guid workspaceId, Guid memberId, string newRole, Guid currentUserId);
    Task<IEnumerable<WorkspaceMemberDto>> GetWorkspaceMembersAsync(Guid workspaceId);
    Task<bool> IsUserInWorkspaceAsync(Guid userId, Guid workspaceId);
    Task<bool> IsUserWorkspaceAdminAsync(Guid userId, Guid workspaceId);
}