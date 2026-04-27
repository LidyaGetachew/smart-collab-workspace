using Microsoft.EntityFrameworkCore;
using SmartCollab.Core.DTOs;
using SmartCollab.Core.Entities;
using SmartCollab.Core.Interfaces;
using SmartCollab.Infrastructure.Data;

namespace SmartCollab.Infrastructure.Services;

public class WorkspaceService : IWorkspaceService
{
    private readonly ApplicationDbContext _context;
    private readonly IEventPublisher _eventPublisher;

    public WorkspaceService(ApplicationDbContext context, IEventPublisher eventPublisher)
    {
        _context = context;
        _eventPublisher = eventPublisher;
    }

    public async Task<IEnumerable<WorkspaceResponseDto>> GetUserWorkspacesAsync(Guid userId)
    {
        return await _context.WorkspaceMembers
            .Where(wm => wm.UserId == userId)
            .Include(wm => wm.Workspace)
                .ThenInclude(w => w.Owner)
            .Include(wm => wm.Workspace)
                .ThenInclude(w => w.Members)
            .Include(wm => wm.Workspace)
                .ThenInclude(w => w.Tasks)
            .Include(wm => wm.Workspace)
                .ThenInclude(w => w.Files)
            .Select(wm => new WorkspaceResponseDto
            {
                Id = wm.Workspace.Id,
                Name = wm.Workspace.Name,
                Description = wm.Workspace.Description,
                OwnerName = $"{wm.Workspace.Owner.FirstName} {wm.Workspace.Owner.LastName}",
                OwnerId = wm.Workspace.OwnerId,
                CreatedAt = wm.Workspace.CreatedAt,
                MemberCount = wm.Workspace.Members.Count,
                TaskCount = wm.Workspace.Tasks.Count,
                FileCount = wm.Workspace.Files.Count
            }).ToListAsync();
    }

    public async Task<WorkspaceResponseDto?> GetWorkspaceByIdAsync(Guid workspaceId)
    {
        var workspace = await _context.Workspaces
            .Include(w => w.Owner)
            .Include(w => w.Members)
            .Include(w => w.Tasks)
            .FirstOrDefaultAsync(w => w.Id == workspaceId);

        if (workspace == null) return null;

        return new WorkspaceResponseDto
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Description = workspace.Description,
            OwnerName = $"{workspace.Owner.FirstName} {workspace.Owner.LastName}",
            OwnerId = workspace.OwnerId,
            CreatedAt = workspace.CreatedAt,
            MemberCount = workspace.Members.Count,
            TaskCount = workspace.Tasks.Count
        };
    }

    public async Task<WorkspaceResponseDto?> CreateWorkspaceAsync(Guid userId, CreateWorkspaceDto dto)
    {
        var workspace = new Workspace
        {
            Name = dto.Name,
            Description = dto.Description,
            OwnerId = userId,
            CreatedAt = DateTime.UtcNow
        };

        _context.Workspaces.Add(workspace);
        await _context.SaveChangesAsync();

        // Add owner as Admin member
        _context.WorkspaceMembers.Add(new WorkspaceMember
        {
            WorkspaceId = workspace.Id,
            UserId = userId,
            Role = "Admin",
            JoinedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        var owner = await _context.Users.FindAsync(userId);

        // Add activity log
        _context.ActivityLogs.Add(new ActivityLog
        {
            WorkspaceId = workspace.Id,
            UserId = userId,
            Action = "created",
            Description = $"Created workspace '{workspace.Name}'",
            CreatedAt = DateTime.UtcNow
        });
        await _context.SaveChangesAsync();

        return new WorkspaceResponseDto
        {
            Id = workspace.Id,
            Name = workspace.Name,
            Description = workspace.Description,
            OwnerName = $"{owner?.FirstName} {owner?.LastName}",
            OwnerId = userId,
            CreatedAt = workspace.CreatedAt,
            MemberCount = 1,
            TaskCount = 0
        };
    }

    public async Task<WorkspaceResponseDto?> UpdateWorkspaceAsync(Guid workspaceId, UpdateWorkspaceDto dto)
    {
        var workspace = await _context.Workspaces.FindAsync(workspaceId);
        if (workspace == null) return null;

        workspace.Name = dto.Name;
        workspace.Description = dto.Description;
        workspace.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();
        return await GetWorkspaceByIdAsync(workspaceId);
    }

    public async Task<bool> DeleteWorkspaceAsync(Guid workspaceId, Guid userId)
    {
        var workspace = await _context.Workspaces
            .Include(w => w.Members)
            .Include(w => w.Tasks)
            .Include(w => w.Files)
            .FirstOrDefaultAsync(w => w.Id == workspaceId);

        if (workspace == null || workspace.OwnerId != userId) return false;

        _context.Workspaces.Remove(workspace);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<(bool Success, string Message)> InviteMemberAsync(Guid workspaceId, Guid invitedByUserId, string targetEmail, string role)
    {
        var inviter = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(wm => wm.WorkspaceId == workspaceId && wm.UserId == invitedByUserId);

        if (inviter?.Role != "Admin") return (false, "Only admins can invite members.");

        var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == targetEmail);
        if (targetUser == null) return (false, "User with this email not found. They must register first.");

        if (await _context.WorkspaceMembers.AnyAsync(wm => wm.WorkspaceId == workspaceId && wm.UserId == targetUser.Id))
            return (false, "User is already a member of this workspace.");

        _context.WorkspaceMembers.Add(new WorkspaceMember
        {
            WorkspaceId = workspaceId,
            UserId = targetUser.Id,
            Role = role,
            JoinedAt = DateTime.UtcNow
        });

        // Add activity log
        _context.ActivityLogs.Add(new ActivityLog
        {
            WorkspaceId = workspaceId,
            UserId = invitedByUserId,
            Action = "invited",
            Description = $"Invited {targetUser.Email} as {role}",
            CreatedAt = DateTime.UtcNow
        });

        await _context.SaveChangesAsync();
        return (true, "Invitation sent");
    }

    public async Task<bool> RemoveMemberAsync(Guid workspaceId, Guid memberId, Guid currentUserId)
    {
        var currentUser = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(wm => wm.WorkspaceId == workspaceId && wm.UserId == currentUserId);

        if (currentUser?.Role != "Admin") return false;

        var member = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(wm => wm.WorkspaceId == workspaceId && wm.Id == memberId);

        if (member == null || member.UserId == currentUserId) return false;

        _context.WorkspaceMembers.Remove(member);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> UpdateMemberRoleAsync(Guid workspaceId, Guid memberId, string newRole, Guid currentUserId)
    {
        var currentUser = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(wm => wm.WorkspaceId == workspaceId && wm.UserId == currentUserId);

        if (currentUser?.Role != "Admin") return false;

        var member = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(wm => wm.WorkspaceId == workspaceId && wm.Id == memberId);

        if (member == null) return false;

        member.Role = newRole;
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<IEnumerable<WorkspaceMemberDto>> GetWorkspaceMembersAsync(Guid workspaceId)
    {
        return await _context.WorkspaceMembers
            .Where(wm => wm.WorkspaceId == workspaceId)
            .Include(wm => wm.User)
            .Select(wm => new WorkspaceMemberDto
            {
                Id = wm.Id,
                UserId = wm.UserId,
                UserName = $"{wm.User.FirstName} {wm.User.LastName}",
                UserEmail = wm.User.Email,
                UserAvatar = wm.User.AvatarUrl,
                Role = wm.Role,
                JoinedAt = wm.JoinedAt
            }).ToListAsync();
    }

    public async Task<bool> IsUserInWorkspaceAsync(Guid userId, Guid workspaceId)
        => await _context.WorkspaceMembers.AnyAsync(wm => wm.WorkspaceId == workspaceId && wm.UserId == userId);

    public async Task<bool> IsUserWorkspaceAdminAsync(Guid userId, Guid workspaceId)
    {
        var member = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(wm => wm.WorkspaceId == workspaceId && wm.UserId == userId);
        return member != null && member.Role == "Admin";
    }
}