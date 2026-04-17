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
                CreatedAt = wm.Workspace.CreatedAt,
                MemberCount = wm.Workspace.Members.Count,
                TaskCount = wm.Workspace.Tasks.Count
            })
            .ToListAsync();

        return workspaces;
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
            CreatedAt = workspace.CreatedAt,
            MemberCount = 1,
            TaskCount = 0
        };
    }

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

    public async Task<bool> IsUserInWorkspaceAsync(Guid userId, Guid workspaceId)
    {
        return await _context.WorkspaceMembers
            .AnyAsync(wm => wm.WorkspaceId == workspaceId && wm.UserId == userId);
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
}