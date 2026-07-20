using AkGaming.GamelyBot.Domain;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.GamelyBot.Infrastructure.Persistence;

public sealed class GamelyBotDbContext(DbContextOptions<GamelyBotDbContext> options) : DbContext(options)
{
    public DbSet<NotificationInboxItem> Notifications => Set<NotificationInboxItem>();
    public DbSet<NotificationDelivery> Deliveries => Set<NotificationDelivery>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<NotificationInboxItem>(builder =>
        {
            builder.ToTable("NotificationInbox");
            builder.HasKey(item => item.Id);
            builder.HasIndex(item => item.EventId).IsUnique();
            builder.HasIndex(item => new { item.Status, item.NextAttemptAtUtc });
            builder.Property(item => item.Type).HasMaxLength(128);
            builder.Property(item => item.Source).HasMaxLength(128);
            builder.Property(item => item.Status).HasMaxLength(32);
            builder.Property(item => item.LastError).HasMaxLength(4000);
        });

        modelBuilder.Entity<NotificationDelivery>(builder =>
        {
            builder.ToTable("NotificationDeliveries");
            builder.HasKey(item => item.Id);
            builder.HasIndex(item => new { item.NotificationInboxItemId, item.Kind }).IsUnique();
            builder.Property(item => item.Kind).HasMaxLength(32);
            builder.Property(item => item.Target).HasMaxLength(128);
            builder.Property(item => item.Title).HasMaxLength(256);
            builder.Property(item => item.Status).HasMaxLength(32);
            builder.Property(item => item.ExternalMessageId).HasMaxLength(128);
            builder.Property(item => item.LastError).HasMaxLength(4000);
            builder.HasOne(item => item.NotificationInboxItem)
                .WithMany(item => item.Deliveries)
                .HasForeignKey(item => item.NotificationInboxItemId)
                .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
