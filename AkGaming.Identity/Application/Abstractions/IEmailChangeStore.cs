using AkGaming.Identity.Domain.Entities;

namespace AkGaming.Identity.Application.Abstractions;

public interface IEmailChangeStore
{
    Task<EmailChangeRequest?> GetPendingAsync(Guid userId, CancellationToken cancellationToken);
    Task IssueAsync(EmailChangeRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task CancelAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken);
    Task<EmailChangeRequest> ConfirmAsync(string tokenHash, string? ipAddress, CancellationToken cancellationToken);
}
