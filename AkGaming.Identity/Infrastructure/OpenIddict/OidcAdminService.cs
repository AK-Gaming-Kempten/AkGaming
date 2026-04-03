using System.Collections.Immutable;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Contracts.Auth;
using AkGaming.Identity.Domain.Entities;
using AkGaming.Identity.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenIddict.Abstractions;
using OpenIddict.EntityFrameworkCore.Models;

namespace AkGaming.Identity.Infrastructure.OpenIddict;

public sealed class OidcAdminService : IOidcAdminService
{
    private const int BadRequestStatusCode = 400;
    private const int NotFoundStatusCode = 404;
    private const int ConflictStatusCode = 409;
    private const string ManagementApiScope = "management_api";

    private static readonly string[] StandardScopes =
    [
        OpenIddictConstants.Scopes.OpenId,
        OpenIddictConstants.Scopes.Profile,
        OpenIddictConstants.Scopes.Email,
        OpenIddictConstants.Scopes.OfflineAccess,
        "roles"
    ];

    private readonly AuthDbContext _dbContext;
    private readonly IIdentityRepository _repository;
    private readonly IOpenIddictApplicationManager _applicationManager;
    private readonly IOpenIddictScopeManager _scopeManager;
    private readonly ILogger<OidcAdminService> _logger;
    private readonly HashSet<string> _protectedClientIds;
    private readonly HashSet<string> _protectedScopeNames;

    public OidcAdminService(
        AuthDbContext dbContext,
        IIdentityRepository repository,
        IOpenIddictApplicationManager applicationManager,
        IOpenIddictScopeManager scopeManager,
        IOptions<OpenIddictSeedOptions> seedOptions,
        ILogger<OidcAdminService> logger)
    {
        _dbContext = dbContext;
        _repository = repository;
        _applicationManager = applicationManager;
        _scopeManager = scopeManager;
        _logger = logger;

        var protectedClientIds = seedOptions.Value.Applications
            .Where(application => application.Scopes.Any(scope => string.Equals(scope, ManagementApiScope, StringComparison.OrdinalIgnoreCase)))
            .Select(application => application.ClientId)
            .Where(clientId => !string.IsNullOrWhiteSpace(clientId));
        _protectedClientIds = new HashSet<string>(protectedClientIds, StringComparer.OrdinalIgnoreCase);

        _protectedScopeNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ManagementApiScope };
        foreach (var scope in seedOptions.Value.Scopes.Where(scope => string.Equals(scope.Name, ManagementApiScope, StringComparison.OrdinalIgnoreCase)))
        {
            if (!string.IsNullOrWhiteSpace(scope.Name))
                _protectedScopeNames.Add(scope.Name);
        }
    }

    public async Task<IReadOnlyList<OidcClientResponse>> GetClientsAsync(CancellationToken cancellationToken)
    {
        var applications = await _dbContext.Set<OpenIddictEntityFrameworkCoreApplication>()
            .OrderBy(application => application.DisplayName ?? application.ClientId)
            .ThenBy(application => application.ClientId)
            .ToListAsync(cancellationToken);

        var result = new List<OidcClientResponse>(applications.Count);
        foreach (var application in applications)
            result.Add(await MapClientAsync(application, cancellationToken));

        return result;
    }

    public async Task<OidcClientResponse> GetClientAsync(string clientId, CancellationToken cancellationToken)
    {
        var application = await FindApplicationAsync(clientId, cancellationToken);
        return await MapClientAsync(application, cancellationToken);
    }

    public async Task<OidcClientResponse> CreateClientAsync(Guid actorUserId, AdminCreateOidcClientRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var clientId = NormalizeClientId(request.ClientId);
        if (await _applicationManager.FindByClientIdAsync(clientId, cancellationToken) is not null)
            throw new AuthException(ConflictStatusCode, "A client with this client id already exists.");

        var descriptor = await BuildApplicationDescriptorAsync(
            clientId,
            request.ClientSecret,
            request.DisplayName,
            request.ClientType,
            request.ConsentType,
            request.RequirePkce,
            request.AllowAuthorizationCodeFlow,
            request.AllowRefreshTokenFlow,
            request.RedirectUris,
            request.PostLogoutRedirectUris,
            request.Scopes,
            requireClientSecretForConfidentialClient: true,
            cancellationToken);

        await _applicationManager.CreateAsync(descriptor, cancellationToken);
        await WriteAuditAsync(
            "admin.oidc.clients.created",
            actorUserId,
            null,
            ipAddress,
            true,
            $"client_id:{clientId};client_type:{descriptor.ClientType};scopes:{string.Join(",", request.Scopes ?? Array.Empty<string>())}",
            cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return await GetClientAsync(clientId, cancellationToken);
    }

    public async Task<OidcClientResponse> UpdateClientAsync(Guid actorUserId, string clientId, AdminUpdateOidcClientRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var application = await FindApplicationAsync(clientId, cancellationToken);
        EnsureClientIsMutable(application.ClientId);

        var descriptor = await BuildApplicationDescriptorAsync(
            application.ClientId!,
            clientSecret: null,
            request.DisplayName,
            request.ClientType,
            request.ConsentType,
            request.RequirePkce,
            request.AllowAuthorizationCodeFlow,
            request.AllowRefreshTokenFlow,
            request.RedirectUris,
            request.PostLogoutRedirectUris,
            request.Scopes,
            requireClientSecretForConfidentialClient: string.IsNullOrWhiteSpace(application.ClientSecret),
            cancellationToken);

        await _applicationManager.PopulateAsync(application, descriptor, cancellationToken);
        await _applicationManager.UpdateAsync(application, cancellationToken);

        if (!string.IsNullOrWhiteSpace(request.NewClientSecret))
            await _applicationManager.UpdateAsync(application, request.NewClientSecret, cancellationToken);

        await WriteAuditAsync(
            "admin.oidc.clients.updated",
            actorUserId,
            null,
            ipAddress,
            true,
            $"client_id:{application.ClientId};client_type:{descriptor.ClientType};scopes:{string.Join(",", request.Scopes ?? Array.Empty<string>())}",
            cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return await MapClientAsync(application, cancellationToken);
    }

    public async Task DeleteClientAsync(Guid actorUserId, string clientId, string? ipAddress, CancellationToken cancellationToken)
    {
        var application = await FindApplicationAsync(clientId, cancellationToken);
        EnsureClientIsMutable(application.ClientId);

        await _applicationManager.DeleteAsync(application, cancellationToken);
        await WriteAuditAsync(
            "admin.oidc.clients.deleted",
            actorUserId,
            null,
            ipAddress,
            true,
            $"client_id:{application.ClientId}",
            cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<OidcScopeResponse>> GetScopesAsync(CancellationToken cancellationToken)
    {
        var scopes = await _dbContext.Set<OpenIddictEntityFrameworkCoreScope>()
            .OrderBy(scope => scope.Name)
            .ToListAsync(cancellationToken);

        var result = new List<OidcScopeResponse>(scopes.Count);
        foreach (var scope in scopes)
            result.Add(await MapScopeAsync(scope, cancellationToken));

        return result;
    }

    public async Task<OidcScopeResponse> GetScopeAsync(string scopeName, CancellationToken cancellationToken)
    {
        var scope = await FindScopeAsync(scopeName, cancellationToken);
        return await MapScopeAsync(scope, cancellationToken);
    }

    public async Task<OidcScopeResponse> CreateScopeAsync(Guid actorUserId, AdminCreateOidcScopeRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var scopeName = NormalizeScopeName(request.Name);
        if (await _scopeManager.FindByNameAsync(scopeName, cancellationToken) is not null)
            throw new AuthException(ConflictStatusCode, "A scope with this name already exists.");

        var descriptor = BuildScopeDescriptor(scopeName, request.DisplayName, request.Description, request.Resources);
        await _scopeManager.CreateAsync(descriptor, cancellationToken);

        await WriteAuditAsync(
            "admin.oidc.scopes.created",
            actorUserId,
            null,
            ipAddress,
            true,
            $"scope:{scopeName};resources:{string.Join(",", descriptor.Resources)}",
            cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return await GetScopeAsync(scopeName, cancellationToken);
    }

    public async Task<OidcScopeResponse> UpdateScopeAsync(Guid actorUserId, string scopeName, AdminUpdateOidcScopeRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var scope = await FindScopeAsync(scopeName, cancellationToken);
        EnsureScopeIsMutable(scope.Name);

        var descriptor = BuildScopeDescriptor(scope.Name!, request.DisplayName, request.Description, request.Resources);
        await _scopeManager.PopulateAsync(scope, descriptor, cancellationToken);
        await _scopeManager.UpdateAsync(scope, cancellationToken);

        await WriteAuditAsync(
            "admin.oidc.scopes.updated",
            actorUserId,
            null,
            ipAddress,
            true,
            $"scope:{scope.Name};resources:{string.Join(",", descriptor.Resources)}",
            cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return await MapScopeAsync(scope, cancellationToken);
    }

    public async Task DeleteScopeAsync(Guid actorUserId, string scopeName, string? ipAddress, CancellationToken cancellationToken)
    {
        var scope = await FindScopeAsync(scopeName, cancellationToken);
        EnsureScopeIsMutable(scope.Name);

        var scopePermission = OpenIddictConstants.Permissions.Prefixes.Scope + scope.Name;
        var inUse = await _dbContext.Set<OpenIddictEntityFrameworkCoreApplication>()
            .AsNoTracking()
            .AnyAsync(application =>
                application.Permissions != null &&
                application.Permissions.Contains(scopePermission),
                cancellationToken);

        if (inUse)
            throw new AuthException(ConflictStatusCode, "Scope is still assigned to one or more clients and cannot be deleted.");

        await _scopeManager.DeleteAsync(scope, cancellationToken);
        await WriteAuditAsync(
            "admin.oidc.scopes.deleted",
            actorUserId,
            null,
            ipAddress,
            true,
            $"scope:{scope.Name}",
            cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<OidcClientResponse> MapClientAsync(OpenIddictEntityFrameworkCoreApplication application, CancellationToken cancellationToken)
    {
        var permissions = (await _applicationManager.GetPermissionsAsync(application, cancellationToken)).ToImmutableArray();
        var requirements = (await _applicationManager.GetRequirementsAsync(application, cancellationToken)).ToImmutableArray();
        var scopes = permissions
            .Where(permission => permission.StartsWith(OpenIddictConstants.Permissions.Prefixes.Scope, StringComparison.Ordinal))
            .Select(permission => permission[OpenIddictConstants.Permissions.Prefixes.Scope.Length..])
            .OrderBy(scope => scope, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var isProtected = IsProtectedClientId(application.ClientId);

        return new OidcClientResponse(
            ClientId: await _applicationManager.GetClientIdAsync(application, cancellationToken) ?? application.ClientId ?? string.Empty,
            DisplayName: await _applicationManager.GetDisplayNameAsync(application, cancellationToken) ?? application.DisplayName ?? application.ClientId ?? string.Empty,
            ClientType: await _applicationManager.GetClientTypeAsync(application, cancellationToken) ?? application.ClientType ?? OpenIddictConstants.ClientTypes.Public,
            ConsentType: await _applicationManager.GetConsentTypeAsync(application, cancellationToken) ?? application.ConsentType ?? OpenIddictConstants.ConsentTypes.Implicit,
            RequirePkce: requirements.Contains(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange, StringComparer.Ordinal),
            AllowAuthorizationCodeFlow: permissions.Contains(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode, StringComparer.Ordinal),
            AllowRefreshTokenFlow: permissions.Contains(OpenIddictConstants.Permissions.GrantTypes.RefreshToken, StringComparer.Ordinal),
            HasClientSecret: !string.IsNullOrWhiteSpace(application.ClientSecret),
            RedirectUris: (await _applicationManager.GetRedirectUrisAsync(application, cancellationToken)).OrderBy(uri => uri, StringComparer.OrdinalIgnoreCase).ToArray(),
            PostLogoutRedirectUris: (await _applicationManager.GetPostLogoutRedirectUrisAsync(application, cancellationToken)).OrderBy(uri => uri, StringComparer.OrdinalIgnoreCase).ToArray(),
            Scopes: scopes,
            IsProtected: isProtected,
            ProtectionReason: isProtected ? "Protected bootstrap/admin client managed from server configuration." : null);
    }

    private async Task<OidcScopeResponse> MapScopeAsync(OpenIddictEntityFrameworkCoreScope scope, CancellationToken cancellationToken)
    {
        var resources = (await _scopeManager.GetResourcesAsync(scope, cancellationToken))
            .OrderBy(resource => resource, StringComparer.OrdinalIgnoreCase)
            .ToArray();
        var isProtected = IsProtectedScopeName(scope.Name);

        return new OidcScopeResponse(
            Name: await _scopeManager.GetNameAsync(scope, cancellationToken) ?? scope.Name ?? string.Empty,
            DisplayName: await _scopeManager.GetDisplayNameAsync(scope, cancellationToken) ?? scope.DisplayName ?? scope.Name ?? string.Empty,
            Description: await _scopeManager.GetDescriptionAsync(scope, cancellationToken) ?? scope.Description,
            Resources: resources,
            IsProtected: isProtected,
            ProtectionReason: isProtected ? "Required by the management tool and restored from server configuration." : null);
    }

    private async Task<OpenIddictApplicationDescriptor> BuildApplicationDescriptorAsync(
        string clientId,
        string? clientSecret,
        string displayName,
        string clientType,
        string consentType,
        bool requirePkce,
        bool allowAuthorizationCodeFlow,
        bool allowRefreshTokenFlow,
        IEnumerable<string>? redirectUris,
        IEnumerable<string>? postLogoutRedirectUris,
        IEnumerable<string>? scopes,
        bool requireClientSecretForConfidentialClient,
        CancellationToken cancellationToken)
    {
        var normalizedClientType = NormalizeClientType(clientType);
        var normalizedConsentType = NormalizeConsentType(consentType);
        var normalizedDisplayName = string.IsNullOrWhiteSpace(displayName) ? clientId : displayName.Trim();
        var normalizedScopes = await NormalizeScopesAsync(scopes, cancellationToken);
        var normalizedRedirectUris = NormalizeAbsoluteUris(redirectUris, "redirectUris");
        var normalizedPostLogoutRedirectUris = NormalizeAbsoluteUris(postLogoutRedirectUris, "postLogoutRedirectUris");

        if (!allowAuthorizationCodeFlow)
            throw new AuthException(BadRequestStatusCode, "Authorization code flow is required.");

        if (normalizedClientType == OpenIddictConstants.ClientTypes.Confidential)
        {
            if (string.IsNullOrWhiteSpace(clientSecret) && requireClientSecretForConfidentialClient)
                throw new AuthException(BadRequestStatusCode, "A client secret is required for confidential clients.");
        }
        else
        {
            clientSecret = null;
        }

        if (normalizedScopes.Count == 0)
            throw new AuthException(BadRequestStatusCode, "At least one scope must be configured.");

        var descriptor = new OpenIddictApplicationDescriptor
        {
            ClientId = clientId,
            ClientSecret = string.IsNullOrWhiteSpace(clientSecret) ? null : clientSecret.Trim(),
            DisplayName = normalizedDisplayName,
            ClientType = normalizedClientType,
            ConsentType = normalizedConsentType
        };

        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Authorization);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.Token);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.Endpoints.EndSession);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.AuthorizationCode);
        descriptor.Permissions.Add(OpenIddictConstants.Permissions.ResponseTypes.Code);

        if (allowRefreshTokenFlow)
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.GrantTypes.RefreshToken);

        foreach (var redirectUri in normalizedRedirectUris)
            descriptor.RedirectUris.Add(new Uri(redirectUri, UriKind.Absolute));

        foreach (var postLogoutRedirectUri in normalizedPostLogoutRedirectUris)
            descriptor.PostLogoutRedirectUris.Add(new Uri(postLogoutRedirectUri, UriKind.Absolute));

        foreach (var scope in normalizedScopes)
            descriptor.Permissions.Add(OpenIddictConstants.Permissions.Prefixes.Scope + scope);

        if (requirePkce)
            descriptor.Requirements.Add(OpenIddictConstants.Requirements.Features.ProofKeyForCodeExchange);

        return descriptor;
    }

    private static OpenIddictScopeDescriptor BuildScopeDescriptor(string name, string displayName, string? description, IEnumerable<string>? resources)
    {
        var normalizedResources = NormalizeIdentifiers(resources, "resources");
        if (normalizedResources.Count == 0)
            throw new AuthException(BadRequestStatusCode, "At least one resource must be configured.");

        var descriptor = new OpenIddictScopeDescriptor
        {
            Name = name,
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? name : displayName.Trim(),
            Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim()
        };

        foreach (var resource in normalizedResources)
            descriptor.Resources.Add(resource);

        return descriptor;
    }

    private async Task<IReadOnlyList<string>> NormalizeScopesAsync(IEnumerable<string>? scopes, CancellationToken cancellationToken)
    {
        var normalizedScopes = NormalizeIdentifiers(scopes, "scopes");
        var knownScopes = new HashSet<string>(StandardScopes, StringComparer.OrdinalIgnoreCase);

        var configuredScopes = await _dbContext.Set<OpenIddictEntityFrameworkCoreScope>()
            .AsNoTracking()
            .Select(scope => scope.Name!)
            .ToListAsync(cancellationToken);
        foreach (var scope in configuredScopes)
            knownScopes.Add(scope);

        var unknownScopes = normalizedScopes
            .Where(scope => !knownScopes.Contains(scope))
            .ToArray();
        if (unknownScopes.Length > 0)
            throw new AuthException(BadRequestStatusCode, $"Unknown scopes: {string.Join(", ", unknownScopes)}.");

        return normalizedScopes;
    }

    private static List<string> NormalizeAbsoluteUris(IEnumerable<string>? uris, string fieldName)
    {
        var values = NormalizeIdentifiers(uris, fieldName);
        foreach (var value in values)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
                throw new AuthException(BadRequestStatusCode, $"All {fieldName} must be absolute HTTP or HTTPS URIs.");
        }

        return values;
    }

    private static List<string> NormalizeIdentifiers(IEnumerable<string>? values, string fieldName)
    {
        var normalized = (values ?? Array.Empty<string>())
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Select(value => value.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (normalized.Any(value => value.Length > 2000))
            throw new AuthException(BadRequestStatusCode, $"{fieldName} contains an excessively long value.");

        return normalized;
    }

    private static string NormalizeClientId(string clientId)
    {
        if (string.IsNullOrWhiteSpace(clientId))
            throw new AuthException(BadRequestStatusCode, "Client id is required.");

        var normalized = clientId.Trim();
        if (normalized.Length > 100)
            throw new AuthException(BadRequestStatusCode, "Client id must be 100 characters or fewer.");

        return normalized;
    }

    private static string NormalizeScopeName(string scopeName)
    {
        if (string.IsNullOrWhiteSpace(scopeName))
            throw new AuthException(BadRequestStatusCode, "Scope name is required.");

        var normalized = scopeName.Trim();
        if (normalized.Length > 200)
            throw new AuthException(BadRequestStatusCode, "Scope name must be 200 characters or fewer.");

        return normalized;
    }

    private static string NormalizeClientType(string clientType)
    {
        return clientType.Trim().ToLowerInvariant() switch
        {
            "confidential" => OpenIddictConstants.ClientTypes.Confidential,
            "public" => OpenIddictConstants.ClientTypes.Public,
            _ => throw new AuthException(BadRequestStatusCode, "clientType must be either 'public' or 'confidential'.")
        };
    }

    private static string NormalizeConsentType(string consentType)
    {
        return consentType.Trim().ToLowerInvariant() switch
        {
            "explicit" => OpenIddictConstants.ConsentTypes.Explicit,
            "external" => OpenIddictConstants.ConsentTypes.External,
            "systematic" => OpenIddictConstants.ConsentTypes.Systematic,
            "implicit" => OpenIddictConstants.ConsentTypes.Implicit,
            _ => throw new AuthException(BadRequestStatusCode, "consentType must be implicit, explicit, external, or systematic.")
        };
    }

    private async Task<OpenIddictEntityFrameworkCoreApplication> FindApplicationAsync(string clientId, CancellationToken cancellationToken)
    {
        var normalizedClientId = NormalizeClientId(clientId);
        var application = await _dbContext.Set<OpenIddictEntityFrameworkCoreApplication>()
            .SingleOrDefaultAsync(application => application.ClientId == normalizedClientId, cancellationToken);

        return application ?? throw new AuthException(NotFoundStatusCode, "OIDC client was not found.");
    }

    private async Task<OpenIddictEntityFrameworkCoreScope> FindScopeAsync(string scopeName, CancellationToken cancellationToken)
    {
        var normalizedScopeName = NormalizeScopeName(scopeName);
        var scope = await _dbContext.Set<OpenIddictEntityFrameworkCoreScope>()
            .SingleOrDefaultAsync(scope => scope.Name == normalizedScopeName, cancellationToken);

        return scope ?? throw new AuthException(NotFoundStatusCode, "OIDC scope was not found.");
    }

    private void EnsureClientIsMutable(string? clientId)
    {
        if (IsProtectedClientId(clientId))
            throw new AuthException(ConflictStatusCode, "This client is protected and must be managed from server configuration.");
    }

    private void EnsureScopeIsMutable(string? scopeName)
    {
        if (IsProtectedScopeName(scopeName))
            throw new AuthException(ConflictStatusCode, "This scope is protected and must be managed from server configuration.");
    }

    private bool IsProtectedClientId(string? clientId)
        => !string.IsNullOrWhiteSpace(clientId) && _protectedClientIds.Contains(clientId);

    private bool IsProtectedScopeName(string? scopeName)
        => !string.IsNullOrWhiteSpace(scopeName) && _protectedScopeNames.Contains(scopeName);

    private async Task WriteAuditAsync(
        string eventType,
        Guid actorUserId,
        string? subjectEmail,
        string? ipAddress,
        bool success,
        string? details,
        CancellationToken cancellationToken)
    {
        await _repository.AddAuditLogAsync(new AuditLog
        {
            UserId = actorUserId,
            EventType = eventType,
            SubjectEmail = subjectEmail,
            IpAddress = ipAddress,
            Success = success,
            Details = details
        }, cancellationToken);
        _logger.LogInformation("Recorded audit event {EventType}: {Details}", eventType, details);
    }
}
