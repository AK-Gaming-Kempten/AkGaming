namespace AkGaming.Identity.Domain.Entities;

public sealed class Role
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;

    public ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
    public ICollection<RoleOpenCloudRole> RoleOpenCloudRoles { get; set; } = new List<RoleOpenCloudRole>();
}
