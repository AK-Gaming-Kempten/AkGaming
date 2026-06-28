using AkGaming.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore;

namespace AkGaming.Identity.Infrastructure.Persistence;

public sealed class AuthDbContext : DbContext
{
    public AuthDbContext(DbContextOptions<AuthDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<OpenCloudRole> OpenCloudRoles => Set<OpenCloudRole>();
    public DbSet<RoleOpenCloudRole> RoleOpenCloudRoles => Set<RoleOpenCloudRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<ExternalLogin> ExternalLogins => Set<ExternalLogin>();
    public DbSet<EmailVerificationToken> EmailVerificationTokens => Set<EmailVerificationToken>();
    public DbSet<PasswordResetToken> PasswordResetTokens => Set<PasswordResetToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.UseOpenIddict();

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Email).IsRequired().HasMaxLength(320);
            entity.HasIndex(x => x.Email).IsUnique();
            entity.Property(x => x.Username).IsRequired().HasMaxLength(100);
            entity.Property(x => x.PasswordHash).HasMaxLength(1000);
            entity.Property(x => x.PrivacyPolicyAccepted).HasDefaultValue(false);
        });

        modelBuilder.Entity<Role>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).IsRequired().HasMaxLength(64);
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<UserRole>(entity =>
        {
            entity.HasKey(x => new { x.UserId, x.RoleId });
            entity.HasOne(x => x.User).WithMany(x => x.UserRoles).HasForeignKey(x => x.UserId);
            entity.HasOne(x => x.Role).WithMany(x => x.UserRoles).HasForeignKey(x => x.RoleId);
        });

        modelBuilder.Entity<Permission>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Key).IsRequired().HasMaxLength(160);
            entity.Property(x => x.Application).IsRequired().HasMaxLength(64);
            entity.Property(x => x.Area).IsRequired().HasMaxLength(64);
            entity.Property(x => x.Operation).IsRequired().HasMaxLength(64);
            entity.Property(x => x.Description).IsRequired().HasMaxLength(256);
            entity.HasIndex(x => x.Key).IsUnique();
        });

        modelBuilder.Entity<RolePermission>(entity =>
        {
            entity.HasKey(x => new { x.RoleId, x.PermissionId });
            entity.HasOne(x => x.Role).WithMany(x => x.RolePermissions).HasForeignKey(x => x.RoleId);
            entity.HasOne(x => x.Permission).WithMany(x => x.RolePermissions).HasForeignKey(x => x.PermissionId);
        });

        modelBuilder.Entity<OpenCloudRole>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Key).IsRequired().HasMaxLength(160);
            entity.Property(x => x.Description).IsRequired().HasMaxLength(256);
            entity.HasIndex(x => x.Key).IsUnique();
        });

        modelBuilder.Entity<RoleOpenCloudRole>(entity =>
        {
            entity.HasKey(x => new { x.RoleId, x.OpenCloudRoleId });
            entity.HasOne(x => x.Role).WithMany(x => x.RoleOpenCloudRoles).HasForeignKey(x => x.RoleId);
            entity.HasOne(x => x.OpenCloudRole).WithMany(x => x.RoleOpenCloudRoles).HasForeignKey(x => x.OpenCloudRoleId);
        });

        modelBuilder.Entity<RefreshToken>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasOne(x => x.User).WithMany(x => x.RefreshTokens).HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<ExternalLogin>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Provider).IsRequired().HasMaxLength(64);
            entity.Property(x => x.ProviderUserId).IsRequired().HasMaxLength(256);
            entity.HasIndex(x => new { x.Provider, x.ProviderUserId }).IsUnique();
            entity.HasOne(x => x.User).WithMany(x => x.ExternalLogins).HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<EmailVerificationToken>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasOne(x => x.User).WithMany(x => x.EmailVerificationTokens).HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<PasswordResetToken>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.TokenHash).IsRequired().HasMaxLength(128);
            entity.HasIndex(x => x.TokenHash).IsUnique();
            entity.HasOne(x => x.User).WithMany(x => x.PasswordResetTokens).HasForeignKey(x => x.UserId);
        });

        modelBuilder.Entity<AuditLog>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).IsRequired().HasMaxLength(128);
            entity.Property(x => x.SubjectEmail).HasMaxLength(320);
            entity.Property(x => x.IpAddress).HasMaxLength(128);
            entity.Property(x => x.Details).HasMaxLength(4000);
            entity.HasIndex(x => x.CreatedAtUtc);
            entity.HasOne(x => x.User).WithMany(x => x.AuditLogs).HasForeignKey(x => x.UserId).IsRequired(false);
        });
    }
}
