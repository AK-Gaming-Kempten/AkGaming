using AkGaming.Identity.Contracts.Auth;

namespace AkGaming.Identity.Application.Abstractions;

public interface IEmailChangeService
{
    Task<EmailChangeResponse?> GetPendingAsync(Guid userId, CancellationToken cancellationToken);
    Task RequestAsync(Guid userId, string newEmail, string password, string? ipAddress, CancellationToken cancellationToken);
    Task<string> StartDiscordAsync(Guid userId, string newEmail, CancellationToken cancellationToken);
    Task RequestWithDiscordAsync(Guid userId, string code, string state, string? ipAddress, CancellationToken cancellationToken);
    Task CancelAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken);
    Task ConfirmAsync(string token, string? ipAddress, CancellationToken cancellationToken);
}
