using AkGaming.Management.Modules.BoardManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Management.Modules.BoardManagement.Infrastructure.Persistence;

public sealed class BoardManagementDbContext(DbContextOptions<BoardManagementDbContext> options) : DbContext(options)
{
    public DbSet<BoardMeeting> Meetings => Set<BoardMeeting>();
    public DbSet<BoardAvailability> Availabilities => Set<BoardAvailability>();
    public DbSet<BoardRescheduleProposal> RescheduleProposals => Set<BoardRescheduleProposal>();
    public DbSet<BoardAgendaItem> AgendaItems => Set<BoardAgendaItem>();
    public DbSet<BoardNotificationOutboxMessage> NotificationOutbox => Set<BoardNotificationOutboxMessage>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BoardMeeting>(b => { b.ToTable("BoardMeetings"); b.HasKey(x => x.Id); b.Property(x => x.Title).HasMaxLength(300); b.Property(x => x.Location).HasMaxLength(500); });
        modelBuilder.Entity<BoardAvailability>(b => { b.ToTable("BoardMeetingAvailabilities"); b.HasKey(x => x.Id); b.Property(x => x.DisplayName).HasMaxLength(300); b.HasOne(x => x.Meeting).WithMany(x => x.Availabilities).HasForeignKey(x => x.MeetingId).OnDelete(DeleteBehavior.Cascade); b.HasIndex(x => new { x.MeetingId, x.UserId }).IsUnique(); });
        modelBuilder.Entity<BoardRescheduleProposal>(b => { b.ToTable("BoardMeetingRescheduleProposals"); b.HasKey(x => x.Id); b.Property(x => x.Reason).HasMaxLength(2000); b.Property(x => x.ProposedByDisplayName).HasMaxLength(300); b.HasOne(x => x.Meeting).WithMany(x => x.RescheduleProposals).HasForeignKey(x => x.MeetingId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<BoardAgendaItem>(b => { b.ToTable("BoardAgendaItems"); b.HasKey(x => x.Id); b.Property(x => x.Title).HasMaxLength(500); b.Property(x => x.Description).HasMaxLength(20000); b.HasOne(x => x.Meeting).WithMany(x => x.AgendaItems).HasForeignKey(x => x.MeetingId).OnDelete(DeleteBehavior.SetNull); b.HasIndex(x => new { x.MeetingId, x.Order }); });
        modelBuilder.Entity<BoardNotificationOutboxMessage>(b => { b.ToTable("BoardNotificationOutbox"); b.HasKey(x => x.Id); b.Property(x => x.Type).HasMaxLength(200); b.Property(x => x.PayloadJson).HasColumnType("text"); b.Property(x => x.LastError).HasMaxLength(4000); b.HasIndex(x => x.EventId).IsUnique(); b.HasIndex(x => new { x.ProcessedAtUtc, x.NextAttemptAtUtc, x.CreatedAtUtc }); });
    }
}
