using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;

namespace AkGaming.Identity.Infrastructure.OpenIddict;

public sealed class OpenIddictSeeder
{
    private const string ManagementApiScope = "management_api";

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
            var existingScope = await _scopeManager.FindByNameAsync(scope.Name, cancellationToken);
            if (existingScope is not null)
            {
                if (IsProtectedScope(scope))
                {
                    var protectedDescriptor = BuildScopeDescriptor(scope);
                    await _scopeManager.UpdateAsync(existingScope, protectedDescriptor, cancellationToken);
                    _logger.LogInformation("Updated protected OpenIddict scope {ScopeName} from configuration.", protectedDescriptor.Name);
                }

                continue;
            }

            var descriptor = BuildScopeDescriptor(scope);

            await _scopeManager.CreateAsync(descriptor, cancellationToken);
            _logger.LogInformation("Seeded OpenIddict scope {ScopeName}.", descriptor.Name);
        }

        foreach (var application in _options.Applications.Where(x => !string.IsNullOrWhiteSpace(x.ClientId)))
        {
            var existingApplication = await _applicationManager.FindByClientIdAsync(application.ClientId, cancellationToken);
            if (existingApplication is not null)
            {
                if (IsProtectedApplication(application))
                {
                    var protectedDescriptor = BuildApplicationDescriptor(application);
                    await _applicationManager.UpdateAsync(existingApplication, protectedDescriptor, cancellationToken);

                    if (!string.IsNullOrWhiteSpace(application.ClientSecret))
                        await _applicationManager.UpdateAsync(existingApplication, application.ClientSecret, cancellationToken);

                    _logger.LogInformation("Updated protected OpenIddict application {ClientId} from configuration.", protectedDescriptor.ClientId);
                }

                continue;
            }

            var descriptor = BuildApplicationDescriptor(application);

            await _applicationManager.CreateAsync(descriptor, cancellationToken);
            _logger.LogInformation("Seeded OpenIddict application {ClientId}.", descriptor.ClientId);
        }
    }

    private static OpenIddictApplicationDescriptor BuildApplicationDescriptor(OpenIddictApplicationSeed application)
    {
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
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);

        foreach (var redirectUri in application.RedirectUris.Where(x => Uri.TryCreate(x, UriKind.Absolute, out _)))
            descriptor.RedirectUris.Add(new Uri(redirectUri, UriKind.Absolute));

        foreach (var postLogoutRedirectUri in application.PostLogoutRedirectUris.Where(x => Uri.TryCreate(x, UriKind.Absolute, out _)))
            descriptor.PostLogoutRedirectUris.Add(new Uri(postLogoutRedirectUri, UriKind.Absolute));

        foreach (var scope in application.Scopes.Where(x => !string.IsNullOrWhiteSpace(x)))
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);

        if (application.RequirePkce)
            descriptor.Requirements.Add(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);

        return descriptor;
    }

    private static OpenIddictScopeDescriptor BuildScopeDescriptor(OpenIddictScopeSeed scope)
    {
        var descriptor = new OpenIddictScopeDescriptor
        {
            Name = scope.Name,
            DisplayName = string.IsNullOrWhiteSpace(scope.DisplayName) ? scope.Name : scope.DisplayName,
            Description = scope.Description
        };

        foreach (var resource in scope.Resources.Where(x => !string.IsNullOrWhiteSpace(x)))
            descriptor.Resources.Add(resource);

        return descriptor;
    }

    private static bool IsProtectedApplication(OpenIddictApplicationSeed application)
        => application.Scopes.Any(scope => string.Equals(scope, ManagementApiScope, StringComparison.OrdinalIgnoreCase));

    private static bool IsProtectedScope(OpenIddictScopeSeed scope)
        => string.Equals(scope.Name, ManagementApiScope, StringComparison.OrdinalIgnoreCase);

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
