using SmartCollab.Core.DTOs;
using SmartCollab.Core.Entities;

namespace SmartCollab.Core.Interfaces;

public interface IWorkspaceService
{
    // Workspace CRUD
    Task<IEnumerable<WorkspaceResponseDto>> GetUserWorkspacesAsync(Guid userId);
    Task<WorkspaceResponseDto?> GetWorkspaceByIdAsync(Guid workspaceId);
    Task<WorkspaceResponseDto?> CreateWorkspaceAsync(Guid userId, CreateWorkspaceDto dto);
    Task<WorkspaceResponseDto?> UpdateWorkspaceAsync(Guid workspaceId, UpdateWorkspaceDto dto);
    Task<bool> DeleteWorkspaceAsync(Guid workspaceId, Guid userId);

    // Member Management
    Task<bool> InviteMemberAsync(Guid workspaceId, Guid invitedByUserId, string targetEmail, string role);
    Task<bool> RemoveMemberFromWorkspaceAsync(Guid workspaceId, Guid memberId);
    Task<bool> RemoveMemberFromWorkspaceByUserIdAsync(Guid workspaceId, Guid userId);
    Task<bool> UpdateMemberRoleAsync(Guid workspaceId, Guid memberId, string newRole);
    Task<IEnumerable<WorkspaceMemberDto>> GetWorkspaceMembersAsync(Guid workspaceId);
    Task<WorkspaceMember?> GetWorkspaceMemberByIdAsync(Guid memberId);

    // Permission Checks
    Task<bool> IsUserInWorkspaceAsync(Guid userId, Guid workspaceId);
    Task<bool> IsUserWorkspaceAdminAsync(Guid userId, Guid workspaceId);
    Task<bool> IsUserWorkspaceOwnerAsync(Guid userId, Guid workspaceId);
}