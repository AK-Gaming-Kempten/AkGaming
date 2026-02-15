using System.Net.Mail;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Application.ExternalAuth;
using AkGaming.Identity.Domain.Constants;
using AkGaming.Identity.Domain.Entities;

namespace AkGaming.Identity.Application.Auth;

public sealed class AuthService : IAuthService
{
    private const int AccessDeniedStatusCode = 401;
    private const int BadRequestStatusCode = 400;
    private const int ConflictStatusCode = 409;
    private const string DiscordProvider = "discord";

    private readonly IIdentityRepository _repository;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IDiscordOAuthService _discordOAuthService;
    private readonly IDiscordStateService _discordStateService;
    private readonly IDiscordAuthSettings _discordAuthSettings;

    public AuthService(
        IIdentityRepository repository,
        IPasswordHasherService passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IDiscordOAuthService discordOAuthService,
        IDiscordStateService discordStateService,
        IDiscordAuthSettings discordAuthSettings)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _discordOAuthService = discordOAuthService;
        _discordStateService = discordStateService;
        _discordAuthSettings = discordAuthSettings;
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

        var role = await GetOrCreateDefaultRoleAsync(cancellationToken);

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

    public Task<DiscordStartResponse> GetDiscordStartUrlAsync(CancellationToken cancellationToken)
    {
        var state = _discordStateService.CreateState(new DiscordOAuthState(
            "login",
            null,
            DateTime.UtcNow.AddMinutes(10),
            Guid.NewGuid().ToString("N")));

        string authorizationUrl;
        try
        {
            authorizationUrl = _discordOAuthService.BuildAuthorizationUrl(state);
        }
        catch (InvalidOperationException exception)
        {
            throw new AuthException(500, exception.Message);
        }

        return Task.FromResult(new DiscordStartResponse(authorizationUrl));
    }

    public Task<DiscordStartResponse> GetDiscordLinkUrlAsync(Guid userId, CancellationToken cancellationToken)
    {
        var state = _discordStateService.CreateState(new DiscordOAuthState(
            "link",
            userId,
            DateTime.UtcNow.AddMinutes(10),
            Guid.NewGuid().ToString("N")));

        string authorizationUrl;
        try
        {
            authorizationUrl = _discordOAuthService.BuildAuthorizationUrl(state);
        }
        catch (InvalidOperationException exception)
        {
            throw new AuthException(500, exception.Message);
        }

        return Task.FromResult(new DiscordStartResponse(authorizationUrl));
    }

    public async Task<DiscordCallbackResponse> HandleDiscordCallbackAsync(string code, string state, string? ipAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(state))
        {
            throw new AuthException(BadRequestStatusCode, "Discord callback is missing required parameters.");
        }

        var oauthState = _discordStateService.ReadState(state);
        if (oauthState is null || oauthState.ExpiresAtUtc <= DateTime.UtcNow)
        {
            throw new AuthException(BadRequestStatusCode, "Discord state is invalid or expired.");
        }

        DiscordIdentity discordIdentity;
        try
        {
            discordIdentity = await _discordOAuthService.GetIdentityFromAuthorizationCodeAsync(code, cancellationToken);
        }
        catch (InvalidOperationException exception)
        {
            throw new AuthException(AccessDeniedStatusCode, exception.Message);
        }

        var existingExternalLogin = await _repository.GetExternalLoginAsync(DiscordProvider, discordIdentity.UserId, cancellationToken);

        if (oauthState.Purpose == "link")
        {
            return await LinkDiscordToExistingUserAsync(oauthState, discordIdentity, existingExternalLogin, cancellationToken);
        }

        return await LoginOrRegisterWithDiscordAsync(discordIdentity, existingExternalLogin, ipAddress, cancellationToken);
    }

    private async Task<DiscordCallbackResponse> LinkDiscordToExistingUserAsync(
        DiscordOAuthState oauthState,
        DiscordIdentity discordIdentity,
        ExternalLogin? existingExternalLogin,
        CancellationToken cancellationToken)
    {
        if (oauthState.UserId is null)
        {
            throw new AuthException(BadRequestStatusCode, "Link state is missing the target user.");
        }

        var targetUser = await _repository.GetUserByIdAsync(oauthState.UserId.Value, cancellationToken);
        if (targetUser is null)
        {
            throw new AuthException(AccessDeniedStatusCode, "Target user account was not found.");
        }

        if (existingExternalLogin is not null && existingExternalLogin.UserId != targetUser.Id)
        {
            throw new AuthException(ConflictStatusCode, "This Discord account is already linked to another account.");
        }

        if (existingExternalLogin is null)
        {
            await _repository.AddExternalLoginAsync(new ExternalLogin
            {
                UserId = targetUser.Id,
                Provider = DiscordProvider,
                ProviderUserId = discordIdentity.UserId,
                ProviderUsername = discordIdentity.Username
            }, cancellationToken);

            await _repository.SaveChangesAsync(cancellationToken);
        }

        return new DiscordCallbackResponse(
            true,
            false,
            discordIdentity.UserId,
            discordIdentity.Username,
            null,
            "Discord account linked successfully.");
    }

    private async Task<DiscordCallbackResponse> LoginOrRegisterWithDiscordAsync(
        DiscordIdentity discordIdentity,
        ExternalLogin? existingExternalLogin,
        string? ipAddress,
        CancellationToken cancellationToken)
    {
        User user;
        var createdUser = false;

        if (existingExternalLogin is not null)
        {
            user = await _repository.GetUserByIdAsync(existingExternalLogin.UserId, cancellationToken)
                ?? throw new AuthException(AccessDeniedStatusCode, "Linked user account was not found.");
        }
        else
        {
            var normalizedDiscordEmail = NormalizeEmailOptional(discordIdentity.Email);
            User? resolvedUser = null;
            if (normalizedDiscordEmail is not null)
            {
                resolvedUser = await _repository.GetUserByEmailAsync(normalizedDiscordEmail, cancellationToken);
            }

            if (resolvedUser is not null && _discordAuthSettings.RequireManualLinkForExistingEmail)
            {
                throw new AuthException(ConflictStatusCode, "An account with this email already exists. Sign in with your existing method and use /auth/discord/link.");
            }

            if (resolvedUser is null)
            {
                if (!_discordAuthSettings.AutoCreateUser)
                {
                    throw new AuthException(ConflictStatusCode, "No linked account found. Link Discord from an existing account first.");
                }

                var role = await GetOrCreateDefaultRoleAsync(cancellationToken);
                var email = normalizedDiscordEmail ?? $"discord-{discordIdentity.UserId}@users.akgaming.local";

                resolvedUser = new User
                {
                    Email = email,
                    PasswordHash = null,
                    IsEmailVerified = normalizedDiscordEmail is not null
                };

                resolvedUser.UserRoles.Add(new UserRole { User = resolvedUser, Role = role });

                await _repository.AddUserAsync(resolvedUser, cancellationToken);
                createdUser = true;
            }

            user = resolvedUser;
            await _repository.AddExternalLoginAsync(new ExternalLogin
            {
                UserId = user.Id,
                Provider = DiscordProvider,
                ProviderUserId = discordIdentity.UserId,
                ProviderUsername = discordIdentity.Username
            }, cancellationToken);
        }

        var tokens = await IssueTokensAsync(user, ipAddress, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return new DiscordCallbackResponse(
            true,
            createdUser,
            discordIdentity.UserId,
            discordIdentity.Username,
            tokens,
            createdUser ? "Discord login succeeded and a new account was created." : "Discord login succeeded.");
    }

    private async Task<Role> GetOrCreateDefaultRoleAsync(CancellationToken cancellationToken)
    {
        var role = await _repository.GetRoleByNameAsync(RoleNames.User, cancellationToken);
        if (role is not null)
        {
            return role;
        }

        role = new Role { Name = RoleNames.User };
        await _repository.AddRoleAsync(role, cancellationToken);
        return role;
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

    private static string? NormalizeEmailOptional(string? email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        try
        {
            return NormalizeEmail(email);
        }
        catch
        {
            return null;
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
