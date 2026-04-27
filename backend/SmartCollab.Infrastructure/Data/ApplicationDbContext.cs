using Microsoft.EntityFrameworkCore;
using SmartCollab.Core.Entities;

namespace SmartCollab.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<User> Users => Set<User>();
    public DbSet<Workspace> Workspaces => Set<Workspace>();
    public DbSet<WorkspaceMember> WorkspaceMembers => Set<WorkspaceMember>();
    public DbSet<TaskItem> Tasks => Set<TaskItem>();
    public DbSet<Comment> Comments => Set<Comment>();
    public DbSet<FileEntity> Files => Set<FileEntity>();
    public DbSet<ChatMessage> ChatMessages => Set<ChatMessage>();
    public DbSet<ActivityLog> ActivityLogs => Set<ActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.Property(e => e.Email).IsRequired().HasMaxLength(255);
            entity.Property(e => e.FirstName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.LastName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.Role).HasDefaultValue("Member");
        });

        // Workspace configuration
        modelBuilder.Entity<Workspace>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Description).HasMaxLength(1000);

            entity.HasOne(e => e.Owner)
                .WithMany(e => e.OwnedWorkspaces)
                .HasForeignKey(e => e.OwnerId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // WorkspaceMember configuration
        modelBuilder.Entity<WorkspaceMember>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("WorkspaceMembers");
            entity.HasIndex(e => new { e.WorkspaceId, e.UserId }).IsUnique();
            entity.Property(e => e.Role).HasDefaultValue("Member");

            entity.HasOne(e => e.Workspace)
                .WithMany(e => e.Members)
                .HasForeignKey(e => e.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(e => e.WorkspaceMembers)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Task configuration
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Tasks");
            entity.Property(e => e.Title).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Status).HasDefaultValue("Todo");
            entity.Property(e => e.Priority).HasDefaultValue(2);

            entity.HasOne(e => e.Workspace)
                .WithMany(e => e.Tasks)
                .HasForeignKey(e => e.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.AssignedTo)
                .WithMany(e => e.AssignedTasks)
                .HasForeignKey(e => e.AssignedToId)
                .OnDelete(DeleteBehavior.SetNull);

            entity.HasOne(e => e.CreatedBy)
                .WithMany(e => e.CreatedTasks)
                .HasForeignKey(e => e.CreatedById)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Comment configuration
        modelBuilder.Entity<Comment>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Content).IsRequired().HasMaxLength(1000);

            entity.HasOne(e => e.Task)
                .WithMany(e => e.Comments)
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Author)
                .WithMany(e => e.Comments)
                .HasForeignKey(e => e.AuthorId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // File configuration
        modelBuilder.Entity<FileEntity>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.ToTable("Files");
            entity.Property(e => e.FileName).IsRequired().HasMaxLength(255);

            entity.HasOne(e => e.Workspace)
                .WithMany(e => e.Files)
                .HasForeignKey(e => e.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.UploadedBy)
                .WithMany(e => e.UploadedFiles)
                .HasForeignKey(e => e.UploadedById)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ChatMessage configuration
        modelBuilder.Entity<ChatMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.WorkspaceId);
            entity.HasIndex(e => e.SentAt);
            entity.Property(e => e.Message).IsRequired();
            entity.Property(e => e.MessageType).HasDefaultValue("text");

            entity.HasOne(e => e.Workspace)
                .WithMany(e => e.ChatMessages)
                .HasForeignKey(e => e.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(e => e.ChatMessages)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // ActivityLog configuration
        modelBuilder.Entity<ActivityLog>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.WorkspaceId);
            entity.HasIndex(e => e.CreatedAt);

            entity.HasOne(e => e.Workspace)
                .WithMany(e => e.Activities)
                .HasForeignKey(e => e.WorkspaceId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.User)
                .WithMany(e => e.Activities)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(e => e.Task)
                .WithMany(e => e.Activities)
                .HasForeignKey(e => e.TaskId)
                .OnDelete(DeleteBehavior.SetNull);
        });
    }
}