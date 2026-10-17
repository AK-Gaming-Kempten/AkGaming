using AkGaming.Gamenight.Application;
using AkGaming.Gamenight.Domain;
using Microsoft.EntityFrameworkCore;
namespace AkGaming.Gamenight.Infrastructure;

public sealed class GamenightDbContext(DbContextOptions<GamenightDbContext> options) : DbContext(options), IGamenightStore
{
    public DbSet<GamenightEvent> Events => Set<GamenightEvent>();
    public DbSet<EventSelection> Selections => Set<EventSelection>();
    public DbSet<Registration> Registrations => Set<Registration>();
    public DbSet<AuditEntry> Audit => Set<AuditEntry>();
    public DbSet<EmailOutbox> Emails => Set<EmailOutbox>();
    protected override void OnModelCreating(ModelBuilder model)
    {
        model.Entity<GamenightEvent>().Property(e => e.Version).IsConcurrencyToken();
        model.Entity<EventSelection>().Property(e => e.Version).IsConcurrencyToken();
        model.Entity<EventSelection>().HasData(new EventSelection { Id = 1, Version = new Guid("237c513f-48ae-4b2d-b6bd-a17c3338a1e0") });
        model.Entity<EventSelection>().HasOne<GamenightEvent>().WithMany().HasForeignKey(s => s.EventId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Registration>().Property(r => r.Version).IsConcurrencyToken();
        model.Entity<Registration>().HasIndex(r => new { r.EventId, r.NormalizedEmail }).IsUnique();
        model.Entity<Registration>().HasOne<GamenightEvent>().WithMany().HasForeignKey(r => r.EventId).OnDelete(DeleteBehavior.Restrict);
        model.Entity<Registration>().Property(r => r.NormalizedEmail).HasMaxLength(254);
        // UTC DateTime and integer cents are translatable by both supported providers.
        model.Entity<EmailOutbox>().HasIndex(e => new { e.SentAt, e.NextAttemptAt });
    }
    public Task<EventSelection> SelectionAsync(CancellationToken ct) => Selections.SingleAsync(s => s.Id == 1, ct);
    public Task<GamenightEvent?> EventAsync(Guid id, CancellationToken ct) => Events.SingleOrDefaultAsync(e => e.Id == id, ct);
    public Task<List<GamenightEvent>> EventsAsync(CancellationToken ct) => Events.ToListAsync(ct);
    public Task<Registration?> RegistrationAsync(Guid id, CancellationToken ct) => Registrations.SingleOrDefaultAsync(r => r.Id == id, ct);
    public Task<List<Registration>> RegistrationsAsync(Guid eventId, CancellationToken ct) => Registrations.Where(r => r.EventId == eventId).ToListAsync(ct);
    void IGamenightStore.Add(object entity) => Add(entity);
    public async Task SaveAsync(CancellationToken ct)
    {
        try { await SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { throw new GamenightException(409, "Die Daten wurden gleichzeitig geändert. Bitte neu laden."); }
        catch (DbUpdateException ex) when (ex.InnerException is Microsoft.Data.Sqlite.SqliteException { SqliteErrorCode: 19 } || ex.InnerException is Npgsql.PostgresException { SqlState: "23505" })
        { throw new GamenightException(409, "Die Anmeldung ist bereits vorhanden oder wurde gleichzeitig geändert."); }
    }
}

