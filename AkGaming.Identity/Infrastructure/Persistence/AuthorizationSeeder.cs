using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using AkGaming.Identity.Domain.Constants;
using AkGaming.Identity.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Identity.Infrastructure.Persistence;

public sealed class AuthorizationSeeder
{
    private readonly AuthDbContext _dbContext;

    public AuthorizationSeeder(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var permissionsByKey = await _dbContext.Permissions
            .ToDictionaryAsync(x => x.Key, StringComparer.Ordinal, cancellationToken);

        foreach (var definition in PermissionCatalog.All)
        {
            if (permissionsByKey.ContainsKey(definition.Key))
            {
                continue;
            }

            var permission = new Permission
            {
                Key = definition.Key,
                Application = definition.Application,
                Area = definition.Area,
                Operation = definition.Operation,
                Description = definition.Description
            };
            _dbContext.Permissions.Add(permission);
            permissionsByKey.Add(permission.Key, permission);
        }

        var openCloudRolesByKey = await _dbContext.OpenCloudRoles
            .ToDictionaryAsync(x => x.Key, StringComparer.Ordinal, cancellationToken);

        foreach (var definition in OpenCloudRoleCatalog.All)
        {
            if (openCloudRolesByKey.ContainsKey(definition.Key))
            {
                continue;
            }

            var openCloudRole = new OpenCloudRole
            {
                Key = definition.Key,
                Description = definition.Description
            };
            _dbContext.OpenCloudRoles.Add(openCloudRole);
            openCloudRolesByKey.Add(openCloudRole.Key, openCloudRole);
        }

        var adminRole = await _dbContext.Roles
            .Include(x => x.RolePermissions)
            .Include(x => x.RoleOpenCloudRoles)
            .SingleOrDefaultAsync(x => x.Name == RoleNames.Admin, cancellationToken);
        if (adminRole is not null)
        {
            var assignedPermissionIds = adminRole.RolePermissions.Select(x => x.PermissionId).ToHashSet();
            foreach (var permission in permissionsByKey.Values.Where(x => !assignedPermissionIds.Contains(x.Id)))
            {
                adminRole.RolePermissions.Add(new RolePermission
                {
                    Role = adminRole,
                    Permission = permission
                });
            }

            var assignedOpenCloudRoleIds = adminRole.RoleOpenCloudRoles.Select(x => x.OpenCloudRoleId).ToHashSet();
            foreach (var openCloudRole in openCloudRolesByKey.Values.Where(x => !assignedOpenCloudRoleIds.Contains(x.Id)))
            {
                adminRole.RoleOpenCloudRoles.Add(new RoleOpenCloudRole
                {
                    Role = adminRole,
                    OpenCloudRole = openCloudRole
                });
            }
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }
}
