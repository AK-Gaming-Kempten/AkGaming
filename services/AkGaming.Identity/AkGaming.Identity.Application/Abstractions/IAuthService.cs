using AkGaming.Identity.Application.Auth;

namespace AkGaming.Identity.Application.Abstractions;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<AuthResponse> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task LogoutAsync(LogoutRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<EmailVerificationResponse> RequestEmailVerificationAsync(EmailVerificationRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task VerifyEmailAsync(VerifyEmailRequest request, string? ipAddress, CancellationToken cancellationToken);
    Task<DiscordStartResponse> GetDiscordStartUrlAsync(CancellationToken cancellationToken);
    Task<DiscordStartResponse> GetDiscordLinkUrlAsync(Guid userId, CancellationToken cancellationToken);
    Task<DiscordCallbackResponse> HandleDiscordCallbackAsync(string code, string state, string? ipAddress, CancellationToken cancellationToken);
}
