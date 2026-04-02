using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace AkGaming.Identity.Infrastructure.OpenIddict;

public sealed class OpenIddictSeeder
{
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly OpenIddictSeedOptions _options;
    private readonly ILogger<OpenIddictSeeder> _logger;

    public OpenIddictSeeder(
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager,
        IOptions<OpenIddictSeedOptions> options,
        ILogger<OpenIddictSeeder> logger)
    {
        _applicationManager = applicationManager;
        _scopeManager = scopeManager;
        _options = options.Value;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        foreach (var scope in _options.Scopes.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
        {
            if (await _scopeManager.FindByNameAsync(scope.Name, cancellationToken) is not null)
            {
                continue;
            }

            var descriptor = new OpenIddictScopeDescriptor
            {
                Name = scope.Name,
                DisplayName = string.IsNullOrWhiteSpace(scope.DisplayName) ? scope.Name : scope.DisplayName,
                Description = scope.Description
            };

            foreach (var resource in scope.Resources.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                descriptor.Resources.Add(resource);
            }

            await _scopeManager.CreateAsync(descriptor, cancellationToken);
            _logger.LogInformation("Seeded OpenIddict scope {ScopeName}.", descriptor.Name);
        }

        foreach (var application in _options.Applications.Where(x => !string.IsNullOrWhiteSpace(x.ClientId)))
        {
            if (await _applicationManager.FindByClientIdAsync(application.ClientId, cancellationToken) is not null)
            {
                continue;
            }

            var descriptor = new OpenIddictApplicationDescriptor
            {
                ClientId = application.ClientId,
                ClientSecret = string.IsNullOrWhiteSpace(application.ClientSecret) ? null : application.ClientSecret,
                DisplayName = string.IsNullOrWhiteSpace(application.DisplayName) ? application.ClientId : application.DisplayName,
                ConsentType = NormalizeConsentType(application.ConsentType),
                ClientType = NormalizeClientType(application.ClientType)
            };

            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.EndSession);

            if (application.AllowAuthorizationCodeFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);
            }

            if (application.AllowRefreshTokenFlow)
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);
            }

            foreach (var redirectUri in application.RedirectUris.Where(x => Uri.TryCreate(x, UriKind.Absolute, out _)))
            {
                descriptor.RedirectUris.Add(new Uri(redirectUri, UriKind.Absolute));
            }

            foreach (var postLogoutRedirectUri in application.PostLogoutRedirectUris.Where(x => Uri.TryCreate(x, UriKind.Absolute, out _)))
            {
                descriptor.PostLogoutRedirectUris.Add(new Uri(postLogoutRedirectUri, UriKind.Absolute));
            }

            foreach (var scope in application.Scopes.Where(x => !string.IsNullOrWhiteSpace(x)))
            {
                descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);
            }

            if (application.RequirePkce)
            {
                descriptor.Requirements.Add(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);
            }

            await _applicationManager.CreateAsync(descriptor, cancellationToken);
            _logger.LogInformation("Seeded OpenIddict application {ClientId}.", descriptor.ClientId);
        }
    }

    private static string NormalizeConsentType(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "explicit" => OpenIddictConstants.ConsentTypes.Explicit,
            "external" => OpenIddictConstants.ConsentTypes.External,
            "systematic" => OpenIddictConstants.ConsentTypes.Systematic,
            _ => OpenIddictConstants.ConsentTypes.Implicit
        };
    }

    private static string NormalizeClientType(string? value)
    {
        return value?.Trim().ToLowerInvariant() switch
        {
            "confidential" => OpenIddictConstants.ClientTypes.Confidential,
            _ => OpenIddictConstants.ClientTypes.Public
        };
    }
}
