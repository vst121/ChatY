using ChatY.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace ChatY.Infrastructure.Data;

public class ChatYDbContext : IdentityDbContext<User>
{
    public ChatYDbContext(DbContextOptions<ChatYDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Chat> Chats { get; set; }
    public DbSet<ChatParticipant> ChatParticipants { get; set; }
    public DbSet<Message> Messages { get; set; }
    public DbSet<MessageMedia> MessageMedia { get; set; }
    public DbSet<MessageFile> MessageFiles { get; set; }
    public DbSet<MessageReaction> MessageReactions { get; set; }
    public DbSet<LinkPreview> LinkPreviews { get; set; }
    public DbSet<PinnedMessage> PinnedMessages { get; set; }
    public DbSet<ChatFolder> ChatFolders { get; set; }
    public DbSet<UserBlock> UserBlocks { get; set; }
    public DbSet<Call> Calls { get; set; }
    public DbSet<CallParticipant> CallParticipants { get; set; }
    public DbSet<TaskItem> Tasks { get; set; }
    public DbSet<TaskAssignee> TaskAssignees { get; set; }
    public DbSet<Poll> Polls { get; set; }
    public DbSet<PollOption> PollOptions { get; set; }
    public DbSet<PollVote> PollVotes { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // User configuration
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.Email).IsUnique();
            entity.HasIndex(e => e.UserName).IsUnique();
            entity.OwnsOne(e => e.PrivacySettings);
        });

        // Chat configuration
        modelBuilder.Entity<Chat>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.CreatedByUserId);
            entity.HasIndex(e => e.FolderId);
            entity.HasOne(e => e.Folder)
                .WithMany(f => f.Chats)
                .HasForeignKey(e => e.FolderId)
                .OnDelete(DeleteBehavior.SetNull);
        });

        // ChatParticipant configuration
        modelBuilder.Entity<ChatParticipant>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.ChatId, e.UserId }).IsUnique();
            entity.HasOne(e => e.Chat)
                .WithMany(c => c.Participants)
                .HasForeignKey(e => e.ChatId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany(u => u.ChatParticipants)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Message configuration
        modelBuilder.Entity<Message>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ChatId);
            entity.HasIndex(e => e.SenderId);
            entity.HasIndex(e => e.ParentMessageId);
            entity.HasIndex(e => e.SentAt);
            entity.HasOne(e => e.Chat)
                .WithMany(c => c.Messages)
                .HasForeignKey(e => e.ChatId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Sender)
                .WithMany(u => u.Messages)
                .HasForeignKey(e => e.SenderId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(e => e.ParentMessage)
                .WithMany(m => m.Replies)
                .HasForeignKey(e => e.ParentMessageId)
                .OnDelete(DeleteBehavior.Restrict);
            entity.OwnsOne(e => e.Receipt);
        });

        // MessageMedia configuration
        modelBuilder.Entity<MessageMedia>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MessageId);
            entity.HasOne(e => e.Message)
                .WithMany(m => m.Media)
                .HasForeignKey(e => e.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MessageFile configuration
        modelBuilder.Entity<MessageFile>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MessageId);
            entity.HasOne(e => e.Message)
                .WithMany(m => m.Files)
                .HasForeignKey(e => e.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // MessageReaction configuration
        modelBuilder.Entity<MessageReaction>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.MessageId, e.UserId, e.Emoji }).IsUnique();
            entity.HasOne(e => e.Message)
                .WithMany(m => m.Reactions)
                .HasForeignKey(e => e.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.User)
                .WithMany(u => u.MessageReactions)
                .HasForeignKey(e => e.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // LinkPreview configuration
        modelBuilder.Entity<LinkPreview>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MessageId).IsUnique();
            entity.HasOne(e => e.Message)
                .WithOne(m => m.LinkPreview)
                .HasForeignKey<LinkPreview>(e => e.MessageId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // PinnedMessage configuration
        modelBuilder.Entity<PinnedMessage>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ChatId);
            entity.HasOne(e => e.Chat)
                .WithMany(c => c.PinnedMessages)
                .HasForeignKey(e => e.ChatId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.Message)
                .WithMany()
                .HasForeignKey(e => e.MessageId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // UserBlock configuration
        modelBuilder.Entity<UserBlock>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.BlockerUserId, e.BlockedUserId }).IsUnique();
            entity.HasOne(e => e.BlockerUser)
                .WithMany(u => u.BlockedUsers)
                .HasForeignKey(e => e.BlockerUserId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(e => e.BlockedUser)
                .WithMany(u => u.BlockedByUsers)
                .HasForeignKey(e => e.BlockedUserId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        // Call configuration
        modelBuilder.Entity<Call>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ChatId);
            entity.HasMany(e => e.Participants)
                .WithOne(p => p.Call)
                .HasForeignKey(p => p.CallId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // TaskItem configuration
        modelBuilder.Entity<TaskItem>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.ChatId);
            entity.HasMany(e => e.Assignees)
                .WithOne(a => a.Task)
                .HasForeignKey(a => a.TaskId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        // Poll configuration
        modelBuilder.Entity<Poll>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => e.MessageId).IsUnique();
            entity.HasMany(e => e.Options)
                .WithOne(o => o.Poll)
                .HasForeignKey(o => o.PollId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasMany(e => e.Votes)
                .WithOne(v => v.Poll)
                .HasForeignKey(v => v.PollId)
                .OnDelete(DeleteBehavior.Restrict);
        });

        modelBuilder.Entity<PollVote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.PollId, e.UserId }).IsUnique();
            entity.HasOne(e => e.Option)
                .WithMany()
                .HasForeignKey(e => e.OptionId)
                .OnDelete(DeleteBehavior.Cascade);
        });

    }
}


