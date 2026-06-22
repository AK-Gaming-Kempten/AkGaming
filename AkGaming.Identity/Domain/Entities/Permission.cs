namespace AkGaming.Identity.Domain.Entities;

public sealed class Permission
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Key { get; set; } = string.Empty;
    public string Application { get; set; } = string.Empty;
    public string Area { get; set; } = string.Empty;
    public string Operation { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
