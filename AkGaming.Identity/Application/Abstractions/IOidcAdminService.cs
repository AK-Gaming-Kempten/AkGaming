using AkGaming.Identity.Contracts.Auth;

namespace AkGaming.Identity.Application.Abstractions;

public interface IOidcAdminService
{
    Task<IReadOnlyList<OidcClientResponse>> GetClientsAsync(CancellationToken cancellationToken);
    Task<OidcClientResponse> GetClientAsync(string clientId, CancellationToken cancellationToken);
    Task<OidcClientResponse> CreateClientAsync(Guid actorUserId, AdminCreateOidcClientRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<OidcClientResponse> UpdateClientAsync(Guid actorUserId, string clientId, AdminUpdateOidcClientRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task DeleteClientAsync(Guid actorUserId, string clientId, string? ipAddress, CancellationToken cancellationToken);

    Task<IReadOnlyList<OidcScopeResponse>> GetScopesAsync(CancellationToken cancellationToken);
    Task<OidcScopeResponse> GetScopeAsync(string scopeName, CancellationToken cancellationToken);
    Task<OidcScopeResponse> CreateScopeAsync(Guid actorUserId, AdminCreateOidcScopeRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<OidcScopeResponse> UpdateScopeAsync(Guid actorUserId, string scopeName, AdminUpdateOidcScopeRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task DeleteScopeAsync(Guid actorUserId, string scopeName, string? ipAddress, CancellationToken cancellationToken);
}
