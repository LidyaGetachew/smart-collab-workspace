using Microsoft.EntityFrameworkCore;
using SmartCollab.Core.DTOs;
using SmartCollab.Core.Entities;
using SmartCollab.Core.Interfaces;
using SmartCollab.Infrastructure.Data;

namespace SmartCollab.Infrastructure.Services;

public class WorkspaceService : IWorkspaceService
{
    private readonly ApplicationDbContext _context;

    public WorkspaceService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ========== WORKSPACE CRUD ==========

    public async Task<IEnumerable<WorkspaceResponseDto>> GetUserWorkspacesAsync(Guid userId)
    {
        var workspaces = await _context.WorkspaceMembers
            .Where(wm => wm.UserId == userId)
            .Include(wm => wm.Workspace)
                .ThenInclude(w => w.Owner)
            .Include(wm => wm.Workspace)
                .ThenInclude(w => w.Members)
            .Include(wm => wm.Workspace)
                .ThenInclude(w => w.Tasks)
            .Select(wm => new WorkspaceResponseDto
            {
                Id = wm.Workspace.Id,
                Name = wm.Workspace.Name,
                Description = wm.Workspace.Description,
                OwnerName = $"{wm.Workspace.Owner.FirstName} {wm.Workspace.Owner.LastName}",
                OwnerId = wm.Workspace.OwnerId,
                CreatedAt = wm.Workspace.CreatedAt,
                MemberCount = wm.Workspace.Members.Count,
                TaskCount = wm.Workspace.Tasks.Count
            })
            .ToListAsync();

        return workspaces;
    }

    public async Task<WorkspaceResponseDto?> GetWorkspaceByIdAsync(Guid workspaceId)
    {
        var workspace = await _context.Workspaces
            .Include(w => w.Owner)
            .Include(w => w.Members)
            .Include(w => w.Tasks)
            .FirstOrDefaultAsync(w => w.Id == workspaceId);

        if (workspace == null)
            return null;

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
        var workspaceMember = new WorkspaceMember
        {
            WorkspaceId = workspace.Id,
            UserId = userId,
            Role = "Admin",
            JoinedAt = DateTime.UtcNow
        };

        _context.WorkspaceMembers.Add(workspaceMember);
        await _context.SaveChangesAsync();

        var owner = await _context.Users.FindAsync(userId);

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

        if (workspace == null)
            return null;

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

        if (workspace == null)
            return false;

        // Only owner can delete workspace
        if (workspace.OwnerId != userId)
            return false;

        // Delete physical files from disk
        var uploadsFolder = Path.Combine(Directory.GetCurrentDirectory(), "uploads", workspaceId.ToString());
        if (Directory.Exists(uploadsFolder))
        {
            Directory.Delete(uploadsFolder, true);
        }

        _context.Workspaces.Remove(workspace);
        await _context.SaveChangesAsync();

        return true;
    }

    // ========== MEMBER MANAGEMENT ==========

    public async Task<bool> InviteMemberAsync(Guid workspaceId, Guid invitedByUserId, string targetEmail, string role)
    {
        // Check if inviter is admin
        var inviterMembership = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(wm => wm.WorkspaceId == workspaceId && wm.UserId == invitedByUserId);

        if (inviterMembership == null || inviterMembership.Role != "Admin")
            return false;

        // Find target user
        var targetUser = await _context.Users.FirstOrDefaultAsync(u => u.Email == targetEmail);
        if (targetUser == null)
            return false;

        // Check if already a member
        var existingMember = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(wm => wm.WorkspaceId == workspaceId && wm.UserId == targetUser.Id);

        if (existingMember != null)
            return false;

        // Add new member
        var newMember = new WorkspaceMember
        {
            WorkspaceId = workspaceId,
            UserId = targetUser.Id,
            Role = role,
            JoinedAt = DateTime.UtcNow
        };

        _context.WorkspaceMembers.Add(newMember);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RemoveMemberFromWorkspaceAsync(Guid workspaceId, Guid memberId)
    {
        var member = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(wm => wm.WorkspaceId == workspaceId && wm.Id == memberId);

        if (member == null)
            return false;

        _context.WorkspaceMembers.Remove(member);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> RemoveMemberFromWorkspaceByUserIdAsync(Guid workspaceId, Guid userId)
    {
        var member = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(wm => wm.WorkspaceId == workspaceId && wm.UserId == userId);

        if (member == null)
            return false;

        _context.WorkspaceMembers.Remove(member);
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> UpdateMemberRoleAsync(Guid workspaceId, Guid memberId, string newRole)
    {
        var member = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(wm => wm.WorkspaceId == workspaceId && wm.Id == memberId);

        if (member == null)
            return false;

        member.Role = newRole;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<IEnumerable<WorkspaceMemberDto>> GetWorkspaceMembersAsync(Guid workspaceId)
    {
        var members = await _context.WorkspaceMembers
            .Where(wm => wm.WorkspaceId == workspaceId)
            .Include(wm => wm.User)
            .Select(wm => new WorkspaceMemberDto
            {
                Id = wm.Id,
                UserId = wm.UserId,
                UserName = $"{wm.User.FirstName} {wm.User.LastName}",
                UserEmail = wm.User.Email,
                Role = wm.Role,
                JoinedAt = wm.JoinedAt
            })
            .ToListAsync();

        return members;
    }

    public async Task<WorkspaceMember?> GetWorkspaceMemberByIdAsync(Guid memberId)
    {
        return await _context.WorkspaceMembers
            .Include(wm => wm.User)
            .FirstOrDefaultAsync(wm => wm.Id == memberId);
    }

    // ========== PERMISSION CHECKS ==========

    public async Task<bool> IsUserInWorkspaceAsync(Guid userId, Guid workspaceId)
    {
        return await _context.WorkspaceMembers
            .AnyAsync(wm => wm.WorkspaceId == workspaceId && wm.UserId == userId);
    }

    public async Task<bool> IsUserWorkspaceAdminAsync(Guid userId, Guid workspaceId)
    {
        var member = await _context.WorkspaceMembers
            .FirstOrDefaultAsync(wm => wm.WorkspaceId == workspaceId && wm.UserId == userId);

        return member != null && member.Role == "Admin";
    }

    public async Task<bool> IsUserWorkspaceOwnerAsync(Guid userId, Guid workspaceId)
    {
        var workspace = await _context.Workspaces
            .FirstOrDefaultAsync(w => w.Id == workspaceId);

        return workspace != null && workspace.OwnerId == userId;
    }
}