namespace AkGaming.Identity.Domain.Constants;

public sealed record OpenCloudRoleDefinition(string Key, string Description);

public static class OpenCloudRoleCatalog
{
    public static IReadOnlyList<OpenCloudRoleDefinition> All { get; } =
    [
        new(OpenCloudRoleNames.Admin, "Full administrator in OpenCloud."),
        new(OpenCloudRoleNames.SpaceAdmin, "Space administrator in OpenCloud."),
        new(OpenCloudRoleNames.User, "Standard OpenCloud user."),
        new(OpenCloudRoleNames.Guest, "Guest access in OpenCloud.")
    ];
}
