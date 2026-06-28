namespace AkGaming.Identity.Domain.Entities;

public sealed class RoleOpenCloudRole
{
    public Guid RoleId { get; set; }
    public Role Role { get; set; } = null!;
    public Guid OpenCloudRoleId { get; set; }
    public OpenCloudRole OpenCloudRole { get; set; } = null!;
}
