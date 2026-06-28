namespace AkGaming.Identity.Domain.Entities;

public sealed class OpenCloudRole
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ICollection<RoleOpenCloudRole> RoleOpenCloudRoles { get; set; } = new List<RoleOpenCloudRole>();
}
