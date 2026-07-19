using AkGaming.Management.Modules.GeneralMeetings.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Management.Modules.GeneralMeetings.Infrastructure.Persistence;

public sealed class GeneralMeetingsDbContext(DbContextOptions<GeneralMeetingsDbContext> options) : DbContext(options)
{
    public DbSet<GeneralMeeting> Meetings => Set<GeneralMeeting>();
    public DbSet<AgendaItem> AgendaItems => Set<AgendaItem>();
    public DbSet<Attendance> Attendances => Set<Attendance>();
    public DbSet<Ballot> Ballots => Set<Ballot>();
    public DbSet<BallotOption> BallotOptions => Set<BallotOption>();
    public DbSet<BallotEntitlement> BallotEntitlements => Set<BallotEntitlement>();
    public DbSet<AnonymousCredential> AnonymousCredentials => Set<AnonymousCredential>();
    public DbSet<AnonymousVote> AnonymousVotes => Set<AnonymousVote>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<GeneralMeeting>(b => { b.ToTable("GeneralMeetings"); b.HasKey(x => x.Id); b.Property(x => x.Title).HasMaxLength(300); b.Property(x => x.Location).HasMaxLength(500); b.Property(x => x.Version).IsConcurrencyToken(); });
        modelBuilder.Entity<AgendaItem>(b => { b.ToTable("GeneralMeetingAgendaItems"); b.HasKey(x => x.Id); b.Property(x => x.Heading).HasMaxLength(500); b.Property(x => x.Description).HasMaxLength(20000); b.Property(x => x.Minutes).HasMaxLength(50000); b.HasOne(x => x.Meeting).WithMany(x => x.AgendaItems).HasForeignKey(x => x.MeetingId).OnDelete(DeleteBehavior.Cascade); b.HasOne(x => x.Parent).WithMany(x => x.Children).HasForeignKey(x => x.ParentId).OnDelete(DeleteBehavior.Restrict); b.HasIndex(x => new { x.MeetingId, x.ParentId, x.Order }); });
        modelBuilder.Entity<Attendance>(b => { b.ToTable("GeneralMeetingAttendances"); b.HasKey(x => x.Id); b.Property(x => x.DisplayName).HasMaxLength(300); b.Property(x => x.MembershipStatus).HasMaxLength(50); b.HasOne(x => x.Meeting).WithMany(x => x.Attendees).HasForeignKey(x => x.MeetingId).OnDelete(DeleteBehavior.Cascade); b.HasIndex(x => new { x.MeetingId, x.MemberId }).IsUnique(); });
        modelBuilder.Entity<Ballot>(b => { b.ToTable("GeneralMeetingBallots"); b.HasKey(x => x.Id); b.Property(x => x.Question).HasMaxLength(2000); b.HasOne(x => x.AgendaItem).WithMany(x => x.Ballots).HasForeignKey(x => x.AgendaItemId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<BallotOption>(b => { b.ToTable("GeneralMeetingBallotOptions"); b.HasKey(x => x.Id); b.Property(x => x.Text).HasMaxLength(1000); b.HasOne(x => x.Ballot).WithMany(x => x.Options).HasForeignKey(x => x.BallotId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<BallotEntitlement>(b => { b.ToTable("GeneralMeetingBallotEntitlements"); b.HasKey(x => x.Id); b.HasIndex(x => new { x.BallotId, x.MemberId }).IsUnique(); b.HasOne<Ballot>().WithMany(x => x.Entitlements).HasForeignKey(x => x.BallotId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<AnonymousCredential>(b => { b.ToTable("GeneralMeetingAnonymousCredentials"); b.HasKey(x => x.Id); b.Property(x => x.TokenHash).HasMaxLength(32); b.HasIndex(x => new { x.BallotId, x.TokenHash }).IsUnique(); b.HasOne<Ballot>().WithMany(x => x.Credentials).HasForeignKey(x => x.BallotId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<AnonymousVote>(b => { b.ToTable("GeneralMeetingAnonymousVotes"); b.HasKey(x => x.Id); b.Property(x => x.SelectionsJson).HasMaxLength(4000); b.HasOne<Ballot>().WithMany(x => x.Votes).HasForeignKey(x => x.BallotId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<MeetingAuditEvent>(b => { b.ToTable("GeneralMeetingAuditEvents"); b.HasKey(x => x.Id); b.Property(x => x.Action).HasMaxLength(100); b.Property(x => x.Details).HasMaxLength(4000); b.HasOne<GeneralMeeting>().WithMany(x => x.AuditEvents).HasForeignKey(x => x.MeetingId).OnDelete(DeleteBehavior.Cascade); b.HasIndex(x => new { x.MeetingId, x.OccurredAt }); });
        modelBuilder.Entity<InvitationDispatch>(b => { b.ToTable("GeneralMeetingInvitationDispatches"); b.HasKey(x => x.Id); b.Property(x => x.Kind).HasMaxLength(30); b.Property(x => x.RecipientEmail).HasMaxLength(320); b.Property(x => x.RecipientName).HasMaxLength(300); b.Property(x => x.Error).HasMaxLength(2000); b.HasOne<GeneralMeeting>().WithMany(x => x.InvitationDispatches).HasForeignKey(x => x.MeetingId).OnDelete(DeleteBehavior.Cascade); });
        modelBuilder.Entity<ProtocolRevision>(b => { b.ToTable("GeneralMeetingProtocolRevisions"); b.HasKey(x => x.Id); b.Property(x => x.Sha256).HasMaxLength(64); b.HasOne<GeneralMeeting>().WithMany(x => x.ProtocolRevisions).HasForeignKey(x => x.MeetingId).OnDelete(DeleteBehavior.Cascade); b.HasIndex(x => new { x.MeetingId, x.Revision }).IsUnique(); });
    }
}
