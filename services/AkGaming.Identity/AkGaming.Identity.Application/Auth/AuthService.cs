using System.Net.Mail;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Domain.Constants;
using AkGaming.Identity.Domain.Entities;

namespace AkGaming.Identity.Application.Auth;

public sealed class AuthService : IAuthService
{
    private const int AccessDeniedStatusCode = 401;
    private const int BadRequestStatusCode = 400;

    private readonly IIdentityRepository _repository;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;

    public AuthService(
        IIdentityRepository repository,
        IPasswordHasherService passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        ValidatePassword(request.Password);

        var existingUser = await _repository.GetUserByEmailAsync(email, cancellationToken);
        if (existingUser is not null)
        {
            throw new AuthException(BadRequestStatusCode, "A user with this email already exists.");
        }

        var role = await _repository.GetRoleByNameAsync(RoleNames.User, cancellationToken);
        if (role is null)
        {
            role = new Role { Name = RoleNames.User };
            await _repository.AddRoleAsync(role, cancellationToken);
        }

        var user = new User
        {
            Email = email,
            PasswordHash = string.Empty,
            IsEmailVerified = false
        };

        user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
        user.UserRoles.Add(new UserRole { User = user, Role = role });

        await _repository.AddUserAsync(user, cancellationToken);

        var response = await IssueTokensAsync(user, ipAddress, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);

        var user = await _repository.GetUserByEmailAsync(email, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            throw new AuthException(AccessDeniedStatusCode, "Invalid credentials.");
        }

        var passwordValid = _passwordHasher.VerifyPassword(user, request.Password, user.PasswordHash);
        if (!passwordValid)
        {
            throw new AuthException(AccessDeniedStatusCode, "Invalid credentials.");
        }

        var response = await IssueTokensAsync(user, ipAddress, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<AuthResponse> RefreshAsync(RefreshRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            throw new AuthException(BadRequestStatusCode, "Refresh token is required.");
        }

        var tokenHash = _refreshTokenService.HashToken(request.RefreshToken);
        var existingToken = await _repository.GetRefreshTokenByHashAsync(tokenHash, cancellationToken);

        if (existingToken is null)
        {
            throw new AuthException(AccessDeniedStatusCode, "Invalid refresh token.");
        }

        if (existingToken.RevokedAtUtc is not null)
        {
            await RevokeAllActiveRefreshTokensAsync(existingToken.UserId, ipAddress, "Refresh token reuse detected.", cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            throw new AuthException(AccessDeniedStatusCode, "Refresh token is no longer valid.");
        }

        if (existingToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            existingToken.RevokedAtUtc = DateTime.UtcNow;
            existingToken.RevokedByIp = ipAddress;
            existingToken.RevocationReason = "Expired token.";
            await _repository.SaveChangesAsync(cancellationToken);
            throw new AuthException(AccessDeniedStatusCode, "Refresh token expired.");
        }

        var user = existingToken.User;
        var newRefreshTokenRaw = _refreshTokenService.GenerateToken();
        var newRefreshTokenHash = _refreshTokenService.HashToken(newRefreshTokenRaw);

        existingToken.RevokedAtUtc = DateTime.UtcNow;
        existingToken.RevokedByIp = ipAddress;
        existingToken.RevocationReason = "Rotated refresh token.";
        existingToken.ReplacedByTokenHash = newRefreshTokenHash;

        await _repository.AddRefreshTokenAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = newRefreshTokenHash,
            ExpiresAtUtc = _refreshTokenService.GetExpiresAtUtc(),
            CreatedByIp = ipAddress
        }, cancellationToken);

        var access = _jwtTokenService.GenerateAccessToken(user);
        await _repository.SaveChangesAsync(cancellationToken);

        return new AuthResponse(access.Token, access.ExpiresAtUtc, newRefreshTokenRaw);
    }

    public async Task LogoutAsync(LogoutRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            return;
        }

        var tokenHash = _refreshTokenService.HashToken(request.RefreshToken);
        var existingToken = await _repository.GetRefreshTokenByHashAsync(tokenHash, cancellationToken);

        if (existingToken is null || existingToken.RevokedAtUtc is not null)
        {
            return;
        }

        existingToken.RevokedAtUtc = DateTime.UtcNow;
        existingToken.RevokedByIp = ipAddress;
        existingToken.RevocationReason = "User logout.";

        await _repository.SaveChangesAsync(cancellationToken);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, string? ipAddress, CancellationToken cancellationToken)
    {
        var access = _jwtTokenService.GenerateAccessToken(user);
        var refreshTokenRaw = _refreshTokenService.GenerateToken();

        await _repository.AddRefreshTokenAsync(new RefreshToken
        {
            UserId = user.Id,
            TokenHash = _refreshTokenService.HashToken(refreshTokenRaw),
            ExpiresAtUtc = _refreshTokenService.GetExpiresAtUtc(),
            CreatedByIp = ipAddress
        }, cancellationToken);

        return new AuthResponse(access.Token, access.ExpiresAtUtc, refreshTokenRaw);
    }

    private async Task RevokeAllActiveRefreshTokensAsync(Guid userId, string? ipAddress, string reason, CancellationToken cancellationToken)
    {
        var activeTokens = await _repository.GetActiveRefreshTokensByUserIdAsync(userId, cancellationToken);

        foreach (var token in activeTokens)
        {
            token.RevokedAtUtc = DateTime.UtcNow;
            token.RevokedByIp = ipAddress;
            token.RevocationReason = reason;
        }
    }

    private static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new AuthException(BadRequestStatusCode, "Email is required.");
        }

        var normalized = email.Trim().ToLowerInvariant();

        try
        {
            _ = new MailAddress(normalized);
            return normalized;
        }
        catch
        {
            throw new AuthException(BadRequestStatusCode, "Invalid email address.");
        }
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password) || password.Length < 8)
        {
            throw new AuthException(BadRequestStatusCode, "Password must be at least 8 characters long.");
        }
    }
}
