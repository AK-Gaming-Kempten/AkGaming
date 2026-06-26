using System.Text.Json;
using AkGaming.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.EntityFrameworkCore;
using OpenIddict.EntityFrameworkCore.Models;

namespace AkGaming.Identity.Api.OpenIddict;

public sealed class OidcRedirectUriCorsPolicyProvider : ICorsPolicyProvider
{
    public const string PolicyName = "BrowserOidcClients";

    private readonly AuthDbContext _dbContext;

    public OidcRedirectUriCorsPolicyProvider(AuthDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<CorsPolicy?> GetPolicyAsync(HttpContext context, string? policyName)
    {
        if (!string.Equals(policyName, PolicyName, StringComparison.Ordinal))
        {
            return null;
        }

        var origins = await GetAllowedOriginsAsync(context.RequestAborted);
        var builder = new CorsPolicyBuilder()
            .AllowAnyHeader()
            .AllowAnyMethod();

        if (origins.Count > 0)
        {
            builder.WithOrigins([.. origins]);
        }

        return builder.Build();
    }

    private async Task<IReadOnlyCollection<string>> GetAllowedOriginsAsync(CancellationToken cancellationToken)
    {
        var applications = await _dbContext.Set<OpenIddictEntityFrameworkCoreApplication>()
            .AsNoTracking()
            .Select(application => new
            {
                application.RedirectUris,
                application.PostLogoutRedirectUris
            })
            .ToListAsync(cancellationToken);

        var origins = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var application in applications)
        {
            AddOrigins(origins, application.RedirectUris);
            AddOrigins(origins, application.PostLogoutRedirectUris);
        }

        return origins;
    }

    private static void AddOrigins(HashSet<string> origins, string? serializedUris)
    {
        if (string.IsNullOrWhiteSpace(serializedUris))
        {
            return;
        }

        try
        {
            foreach (var uriValue in JsonSerializer.Deserialize<string[]>(serializedUris) ?? [])
            {
                if (Uri.TryCreate(uriValue, UriKind.Absolute, out var uri) &&
                    (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
                {
                    origins.Add(uri.GetLeftPart(UriPartial.Authority));
                }
            }
        }
        catch (JsonException)
        {
            // Ignore malformed persisted values so a bad client record cannot break discovery for every client.
        }
    }
}
