using System.Net.Mail;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Application.ExternalAuth;
using AkGaming.Identity.Domain.Constants;
using AkGaming.Identity.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace AkGaming.Identity.Application.Auth;

public sealed class AuthService : IAuthService
{
    private const int AccessDeniedStatusCode = 401;
    private const int BadRequestStatusCode = 400;
    private const int ForbiddenStatusCode = 403;
    private const int NotFoundStatusCode = 404;
    private const int LockedStatusCode = 423;
    private const int ConflictStatusCode = 409;
    private const string DiscordProvider = "discord";

    private readonly IIdentityRepository _repository;
    private readonly IPasswordHasherService _passwordHasher;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IRefreshTokenService _refreshTokenService;
    private readonly IEmailSender _emailSender;
    private readonly IDiscordOAuthService _discordOAuthService;
    private readonly IDiscordStateService _discordStateService;
    private readonly IDiscordAuthSettings _discordAuthSettings;
    private readonly IAuthHardeningSettings _hardeningSettings;
    private readonly IAppUrlSettings _appUrlSettings;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IIdentityRepository repository,
        IPasswordHasherService passwordHasher,
        IJwtTokenService jwtTokenService,
        IRefreshTokenService refreshTokenService,
        IEmailSender emailSender,
        IDiscordOAuthService discordOAuthService,
        IDiscordStateService discordStateService,
        IDiscordAuthSettings discordAuthSettings,
        IAuthHardeningSettings hardeningSettings,
        IAppUrlSettings appUrlSettings,
        ILogger<AuthService> logger)
    {
        _repository = repository;
        _passwordHasher = passwordHasher;
        _jwtTokenService = jwtTokenService;
        _refreshTokenService = refreshTokenService;
        _emailSender = emailSender;
        _discordOAuthService = discordOAuthService;
        _discordStateService = discordStateService;
        _discordAuthSettings = discordAuthSettings;
        _hardeningSettings = hardeningSettings;
        _appUrlSettings = appUrlSettings;
        _logger = logger;
    }

    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        ValidatePassword(request.Password);

        var existingUser = await _repository.GetUserByEmailAsync(email, cancellationToken);
        if (existingUser is not null)
        {
            await WriteAuditAsync("register.failed", null, email, ipAddress, false, "email_exists", cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
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
        await WriteAuditAsync("register.success", user.Id, user.Email, ipAddress, true, null, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return response;
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);

        var user = await _repository.GetUserByEmailAsync(email, cancellationToken);
        if (user is null || string.IsNullOrWhiteSpace(user.PasswordHash))
        {
            await WriteAuditAsync("login.failed", null, email, ipAddress, false, "invalid_credentials", cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            throw new AuthException(AccessDeniedStatusCode, "Invalid credentials.");
        }

        if (user.LockoutEndUtc.HasValue && user.LockoutEndUtc.Value > DateTime.UtcNow)
        {
            await WriteAuditAsync("login.locked", user.Id, user.Email, ipAddress, false, $"locked_until:{user.LockoutEndUtc:O}", cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            throw new AuthException(LockedStatusCode, "Account is temporarily locked due to repeated failed login attempts.");
        }

        var passwordValid = _passwordHasher.VerifyPassword(user, request.Password, user.PasswordHash);
        if (!passwordValid)
        {
            await HandleFailedLoginAttemptAsync(user, ipAddress, cancellationToken);
            throw new AuthException(AccessDeniedStatusCode, "Invalid credentials.");
        }

        if (_hardeningSettings.RequireVerifiedEmailForLogin && !user.IsEmailVerified)
        {
            await WriteAuditAsync("login.unverified", user.Id, user.Email, ipAddress, false, "email_not_verified", cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            throw new AuthException(ForbiddenStatusCode, "Email verification is required before login.");
        }

        user.AccessFailedCount = 0;
        user.LockoutEndUtc = null;

        var response = await IssueTokensAsync(user, ipAddress, cancellationToken);
        await WriteAuditAsync("login.success", user.Id, user.Email, ipAddress, true, null, cancellationToken);
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
            await WriteAuditAsync("refresh.failed", null, null, ipAddress, false, "token_not_found", cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            throw new AuthException(AccessDeniedStatusCode, "Invalid refresh token.");
        }

        if (existingToken.RevokedAtUtc is not null)
        {
            await RevokeAllActiveRefreshTokensAsync(existingToken.UserId, ipAddress, "Refresh token reuse detected.", cancellationToken);
            await WriteAuditAsync("refresh.reuse_detected", existingToken.UserId, existingToken.User.Email, ipAddress, false, "reuse_detected", cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            throw new AuthException(AccessDeniedStatusCode, "Refresh token is no longer valid.");
        }

        if (existingToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            existingToken.RevokedAtUtc = DateTime.UtcNow;
            existingToken.RevokedByIp = ipAddress;
            existingToken.RevocationReason = "Expired token.";
            await WriteAuditAsync("refresh.expired", existingToken.UserId, existingToken.User.Email, ipAddress, false, "token_expired", cancellationToken);
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
        await WriteAuditAsync("refresh.success", user.Id, user.Email, ipAddress, true, null, cancellationToken);
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

        await WriteAuditAsync("logout.success", existingToken.UserId, existingToken.User.Email, ipAddress, true, null, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<CurrentUserResponse> GetCurrentUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _repository.GetUserByIdWithExternalLoginsAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new AuthException(AccessDeniedStatusCode, "User account was not found.");
        }

        var discordLink = user.ExternalLogins
            .Where(x => x.Provider == DiscordProvider)
            .OrderByDescending(x => x.LinkedAtUtc)
            .FirstOrDefault();

        return new CurrentUserResponse(
            user.Id,
            user.Email,
            user.IsEmailVerified,
            user.UserRoles.Select(x => x.Role.Name).ToArray(),
            discordLink is null
                ? null
                : new DiscordLinkInfo(discordLink.ProviderUserId, discordLink.ProviderUsername, discordLink.LinkedAtUtc));
    }

    public async Task<AdminUsersResponse> GetUsersAsync(int page, int pageSize, string? search, CancellationToken cancellationToken)
    {
        if (page < 1)
        {
            throw new AuthException(BadRequestStatusCode, "page must be at least 1.");
        }

        if (pageSize is < 1 or > 200)
        {
            throw new AuthException(BadRequestStatusCode, "pageSize must be between 1 and 200.");
        }

        var skip = (page - 1) * pageSize;
        var totalCount = await _repository.CountUsersAsync(search, cancellationToken);
        var users = await _repository.GetUsersPageAsync(skip, pageSize, search, cancellationToken);

        var items = users
            .Select(user => new AdminUserListItemResponse(
                user.Id,
                user.Email,
                user.IsEmailVerified,
                user.UserRoles.Select(x => x.Role.Name).OrderBy(x => x).ToArray(),
                user.CreatedAtUtc,
                user.LockoutEndUtc))
            .ToArray();

        return new AdminUsersResponse(page, pageSize, totalCount, items);
    }

    public async Task<AdminUserDetailsResponse> GetUserDetailsAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _repository.GetUserByIdWithExternalLoginsAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new AuthException(NotFoundStatusCode, "User account was not found.");
        }

        var discordLink = user.ExternalLogins
            .Where(x => x.Provider == DiscordProvider)
            .OrderByDescending(x => x.LinkedAtUtc)
            .FirstOrDefault();

        return new AdminUserDetailsResponse(
            user.Id,
            user.Email,
            user.IsEmailVerified,
            user.UserRoles.Select(x => x.Role.Name).OrderBy(x => x).ToArray(),
            user.CreatedAtUtc,
            user.LockoutEndUtc,
            discordLink is null
                ? null
                : new DiscordLinkInfo(discordLink.ProviderUserId, discordLink.ProviderUsername, discordLink.LinkedAtUtc));
    }

    public async Task<UserRolesResponse> GetUserRolesAsync(Guid userId, CancellationToken cancellationToken)
    {
        var user = await _repository.GetUserByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new AuthException(NotFoundStatusCode, "User account was not found.");
        }

        return new UserRolesResponse(user.Id, user.UserRoles.Select(x => x.Role.Name).OrderBy(x => x).ToArray());
    }

    public async Task<UserRolesResponse> SetUserRolesAsync(Guid actorUserId, Guid userId, AdminSetUserRolesRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var requestedRoleNames = (request.Roles ?? [])
            .Select(x => x?.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (requestedRoleNames.Length == 0)
        {
            throw new AuthException(BadRequestStatusCode, "At least one role is required.");
        }

        var targetUser = await _repository.GetUserByIdAsync(userId, cancellationToken);
        if (targetUser is null)
        {
            throw new AuthException(NotFoundStatusCode, "Target user account was not found.");
        }

        var requestedRoles = await _repository.GetRolesByNamesAsync(requestedRoleNames, cancellationToken);
        if (requestedRoles.Count != requestedRoleNames.Length)
        {
            var existingRoleNames = requestedRoles.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var unknownRoles = requestedRoleNames.Where(x => !existingRoleNames.Contains(x!));
            throw new AuthException(BadRequestStatusCode, $"Unknown role(s): {string.Join(", ", unknownRoles)}");
        }

        var currentRoleNames = targetUser.UserRoles.Select(x => x.Role.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var requestedRoleNameSet = requestedRoles.Select(x => x.Name).ToHashSet(StringComparer.OrdinalIgnoreCase);

        var removingAdmin = currentRoleNames.Contains(RoleNames.Admin) && !requestedRoleNameSet.Contains(RoleNames.Admin);
        if (removingAdmin)
        {
            var adminCount = await _repository.CountUsersInRoleAsync(RoleNames.Admin, cancellationToken);
            if (adminCount <= 1)
            {
                throw new AuthException(ConflictStatusCode, "Cannot remove the Admin role from the last remaining admin.");
            }
        }

        var rolesToRemove = targetUser.UserRoles.Where(x => !requestedRoleNameSet.Contains(x.Role.Name)).ToList();
        foreach (var roleToRemove in rolesToRemove)
        {
            targetUser.UserRoles.Remove(roleToRemove);
        }

        var currentRoleIdSet = targetUser.UserRoles.Select(x => x.RoleId).ToHashSet();
        foreach (var role in requestedRoles)
        {
            if (currentRoleIdSet.Contains(role.Id))
            {
                continue;
            }

            targetUser.UserRoles.Add(new UserRole
            {
                UserId = targetUser.Id,
                User = targetUser,
                RoleId = role.Id,
                Role = role
            });
        }

        var updatedRoleNames = targetUser.UserRoles.Select(x => x.Role.Name).OrderBy(x => x).ToArray();
        await WriteAuditAsync(
            "admin.roles.updated",
            actorUserId,
            targetUser.Email,
            ipAddress,
            true,
            $"target_user:{targetUser.Id};roles:{string.Join(",", updatedRoleNames)}",
            cancellationToken);

        await _repository.SaveChangesAsync(cancellationToken);
        return new UserRolesResponse(targetUser.Id, updatedRoleNames);
    }

    public async Task<IReadOnlyList<RoleResponse>> GetRolesAsync(CancellationToken cancellationToken)
    {
        var roles = await _repository.GetAllRolesAsync(cancellationToken);
        return roles.Select(x => new RoleResponse(x.Id, x.Name)).ToArray();
    }

    public async Task<RoleResponse> CreateRoleAsync(Guid actorUserId, AdminCreateRoleRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var normalizedName = NormalizeRoleName(request.Name);
        var existingRole = await _repository.GetRoleByNameAsync(normalizedName, cancellationToken);
        if (existingRole is not null)
        {
            throw new AuthException(ConflictStatusCode, "A role with this name already exists.");
        }

        var role = new Role { Name = normalizedName };
        await _repository.AddRoleAsync(role, cancellationToken);

        await WriteAuditAsync("admin.roles.created", actorUserId, null, ipAddress, true, $"role:{role.Name};role_id:{role.Id}", cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return new RoleResponse(role.Id, role.Name);
    }

    public async Task<RoleResponse> RenameRoleAsync(Guid actorUserId, Guid roleId, AdminRenameRoleRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var role = await _repository.GetRoleByIdAsync(roleId, cancellationToken);
        if (role is null)
        {
            throw new AuthException(NotFoundStatusCode, "Role was not found.");
        }

        if (IsSystemRole(role.Name))
        {
            throw new AuthException(ConflictStatusCode, "System roles cannot be renamed.");
        }

        var normalizedName = NormalizeRoleName(request.Name);
        var existingRole = await _repository.GetRoleByNameAsync(normalizedName, cancellationToken);
        if (existingRole is not null && existingRole.Id != role.Id)
        {
            throw new AuthException(ConflictStatusCode, "A role with this name already exists.");
        }

        role.Name = normalizedName;
        await WriteAuditAsync("admin.roles.renamed", actorUserId, null, ipAddress, true, $"role_id:{role.Id};new_name:{role.Name}", cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
        return new RoleResponse(role.Id, role.Name);
    }

    public async Task DeleteRoleAsync(Guid actorUserId, Guid roleId, string? ipAddress, CancellationToken cancellationToken)
    {
        var role = await _repository.GetRoleByIdAsync(roleId, cancellationToken);
        if (role is null)
        {
            throw new AuthException(NotFoundStatusCode, "Role was not found.");
        }

        if (IsSystemRole(role.Name))
        {
            throw new AuthException(ConflictStatusCode, "System roles cannot be deleted.");
        }

        var assignmentCount = await _repository.CountUsersWithRoleIdAsync(roleId, cancellationToken);
        if (assignmentCount > 0)
        {
            throw new AuthException(ConflictStatusCode, "Role is assigned to one or more users and cannot be deleted.");
        }

        _repository.RemoveRole(role);
        await WriteAuditAsync("admin.roles.deleted", actorUserId, null, ipAddress, true, $"role_id:{role.Id};name:{role.Name}", cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
    }

    public async Task<EmailVerificationResponse> RequestEmailVerificationAsync(EmailVerificationRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        var email = NormalizeEmail(request.Email);
        var user = await _repository.GetUserByEmailAsync(email, cancellationToken);

        if (user is null)
        {
            await WriteAuditAsync("email_verification.request", null, email, ipAddress, true, "user_not_found", cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            return new EmailVerificationResponse("If the account exists, a verification message has been issued.");
        }

        if (user.IsEmailVerified)
        {
            await WriteAuditAsync("email_verification.request", user.Id, user.Email, ipAddress, true, "already_verified", cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            return new EmailVerificationResponse("Email is already verified.");
        }

        return await IssueEmailVerificationTokenAsync(user, ipAddress, cancellationToken);
    }

    public async Task<EmailVerificationResponse> RequestEmailVerificationForUserAsync(Guid userId, string? ipAddress, CancellationToken cancellationToken)
    {
        var user = await _repository.GetUserByIdAsync(userId, cancellationToken);
        if (user is null)
        {
            throw new AuthException(AccessDeniedStatusCode, "User account was not found.");
        }

        if (user.IsEmailVerified)
        {
            await WriteAuditAsync("email_verification.request", user.Id, user.Email, ipAddress, true, "already_verified", cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            return new EmailVerificationResponse("Email is already verified.");
        }

        return await IssueEmailVerificationTokenAsync(user, ipAddress, cancellationToken);
    }

    private async Task<EmailVerificationResponse> IssueEmailVerificationTokenAsync(User user, string? ipAddress, CancellationToken cancellationToken)
    {
        var activeTokens = await _repository.GetActiveEmailVerificationTokensByUserIdAsync(user.Id, cancellationToken);
        foreach (var activeToken in activeTokens)
        {
            activeToken.ConsumedAtUtc = DateTime.UtcNow;
        }

        var rawToken = _refreshTokenService.GenerateToken();
        var tokenHash = _refreshTokenService.HashToken(rawToken);
        var verifyLink = BuildEmailVerificationLink(rawToken);

        await _repository.AddEmailVerificationTokenAsync(new EmailVerificationToken
        {
            UserId = user.Id,
            TokenHash = tokenHash,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(_hardeningSettings.EmailVerificationTokenHours),
            CreatedByIp = ipAddress
        }, cancellationToken);

        var subject = "Verify your AK Gaming Identity email";
        var textBody =
            "Hello,\n\n" +
            "Please verify your AK Gaming Identity email address.\n\n" +
            $"Verify instantly: {verifyLink}\n\n" +
            "Or enter this verification token in the identity page:\n" +
            $"{rawToken}\n\n" +
            $"This token expires in {_hardeningSettings.EmailVerificationTokenHours} hour(s).\n\n" +
            "If you did not request this, you can ignore this email.";
        var htmlBody =
            "<div style=\"font-family:Arial,Helvetica,sans-serif;color:#222;line-height:1.6\">" +
            "<h2 style=\"margin:0 0 12px;color:#1f2937\">Verify your AK Gaming Identity email</h2>" +
            "<p style=\"margin:0 0 12px\">Please confirm your email address to secure your account.</p>" +
            $"<p style=\"margin:20px 0\"><a href=\"{verifyLink}\" style=\"background:#286c3f;color:#fff;text-decoration:none;padding:10px 16px;border-radius:4px;display:inline-block;font-weight:600\">Verify Email</a></p>" +
            "<p style=\"margin:0 0 8px\">If the button does not work, use this verification token on the identity page:</p>" +
            $"<p style=\"margin:0 0 12px;font-size:18px;font-weight:700;letter-spacing:0.5px\">{rawToken}</p>" +
            $"<p style=\"margin:0 0 8px;color:#4b5563\">Token expiry: {_hardeningSettings.EmailVerificationTokenHours} hour(s).</p>" +
            "<p style=\"margin:0;color:#6b7280\">If you did not request this, you can ignore this email.</p>" +
            "</div>";

        try
        {
            await _emailSender.SendAsync(user.Email, subject, textBody, htmlBody, cancellationToken);
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Failed to send verification email to {Email}.", user.Email);
            await WriteAuditAsync("email_verification.email_send_failed", user.Id, user.Email, ipAddress, false, $"smtp_send_failed:{exception.GetType().Name}", cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            throw new AuthException(500, "Verification email could not be sent.");
        }

        await WriteAuditAsync("email_verification.issued", user.Id, user.Email, ipAddress, true, null, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        var exposedToken = _hardeningSettings.ExposeEmailVerificationToken ? rawToken : null;
        return new EmailVerificationResponse("Verification token created.", exposedToken);
    }

    public async Task VerifyEmailAsync(VerifyEmailRequest request, string? ipAddress, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
        {
            throw new AuthException(BadRequestStatusCode, "Verification token is required.");
        }

        var tokenHash = _refreshTokenService.HashToken(request.Token);
        var verificationToken = await _repository.GetEmailVerificationTokenByHashAsync(tokenHash, cancellationToken);

        if (verificationToken is null)
        {
            await WriteAuditAsync("email_verification.failed", null, null, ipAddress, false, "token_not_found", cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            throw new AuthException(BadRequestStatusCode, "Verification token is invalid.");
        }

        if (verificationToken.ConsumedAtUtc.HasValue || verificationToken.ExpiresAtUtc <= DateTime.UtcNow)
        {
            await WriteAuditAsync("email_verification.failed", verificationToken.UserId, verificationToken.User.Email, ipAddress, false, "token_invalid_or_expired", cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
            throw new AuthException(BadRequestStatusCode, "Verification token is invalid or expired.");
        }

        verificationToken.ConsumedAtUtc = DateTime.UtcNow;
        verificationToken.User.IsEmailVerified = true;

        await WriteAuditAsync("email_verification.success", verificationToken.UserId, verificationToken.User.Email, ipAddress, true, null, cancellationToken);
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
            return await LinkDiscordToExistingUserAsync(oauthState, discordIdentity, existingExternalLogin, ipAddress, cancellationToken);
        }

        return await LoginOrRegisterWithDiscordAsync(discordIdentity, existingExternalLogin, ipAddress, cancellationToken);
    }

    private async Task<DiscordCallbackResponse> LinkDiscordToExistingUserAsync(
        DiscordOAuthState oauthState,
        DiscordIdentity discordIdentity,
        ExternalLogin? existingExternalLogin,
        string? ipAddress,
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
            await WriteAuditAsync("discord.link.failed", targetUser.Id, targetUser.Email, ipAddress, false, "already_linked_elsewhere", cancellationToken);
            await _repository.SaveChangesAsync(cancellationToken);
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

            await WriteAuditAsync("discord.link.success", targetUser.Id, targetUser.Email, ipAddress, true, $"discord_user:{discordIdentity.UserId}", cancellationToken);
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
                await WriteAuditAsync("discord.login.failed", resolvedUser.Id, resolvedUser.Email, ipAddress, false, "manual_link_required", cancellationToken);
                await _repository.SaveChangesAsync(cancellationToken);
                throw new AuthException(ConflictStatusCode, "An account with this email already exists. Sign in with your existing method and use /auth/discord/link.");
            }

            if (resolvedUser is null)
            {
                if (!_discordAuthSettings.AutoCreateUser)
                {
                    await WriteAuditAsync("discord.login.failed", null, normalizedDiscordEmail, ipAddress, false, "auto_create_disabled", cancellationToken);
                    await _repository.SaveChangesAsync(cancellationToken);
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

        user.AccessFailedCount = 0;
        user.LockoutEndUtc = null;

        var tokens = await IssueTokensAsync(user, ipAddress, cancellationToken);
        await WriteAuditAsync("discord.login.success", user.Id, user.Email, ipAddress, true, createdUser ? "created_user" : "existing_user", cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);

        return new DiscordCallbackResponse(
            true,
            createdUser,
            discordIdentity.UserId,
            discordIdentity.Username,
            tokens,
            createdUser ? "Discord login succeeded and a new account was created." : "Discord login succeeded.");
    }

    private async Task HandleFailedLoginAttemptAsync(User user, string? ipAddress, CancellationToken cancellationToken)
    {
        user.AccessFailedCount++;

        var details = "invalid_credentials";

        if (user.AccessFailedCount >= _hardeningSettings.MaxFailedLoginAttempts)
        {
            user.LockoutEndUtc = DateTime.UtcNow.AddMinutes(_hardeningSettings.LockoutMinutes);
            details = $"locked_until:{user.LockoutEndUtc:O}";
        }

        await WriteAuditAsync("login.failed", user.Id, user.Email, ipAddress, false, details, cancellationToken);
        await _repository.SaveChangesAsync(cancellationToken);
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

    private async Task WriteAuditAsync(
        string eventType,
        Guid? userId,
        string? subjectEmail,
        string? ipAddress,
        bool success,
        string? details,
        CancellationToken cancellationToken)
    {
        await _repository.AddAuditLogAsync(new AuditLog
        {
            UserId = userId,
            EventType = eventType,
            SubjectEmail = subjectEmail,
            IpAddress = ipAddress,
            Success = success,
            Details = details
        }, cancellationToken);
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

    private static string NormalizeRoleName(string roleName)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new AuthException(BadRequestStatusCode, "Role name is required.");
        }

        var normalized = roleName.Trim();
        if (normalized.Length is < 2 or > 64)
        {
            throw new AuthException(BadRequestStatusCode, "Role name must be between 2 and 64 characters.");
        }

        return normalized;
    }

    private static bool IsSystemRole(string roleName)
    {
        return roleName.Equals(RoleNames.Admin, StringComparison.OrdinalIgnoreCase)
               || roleName.Equals(RoleNames.User, StringComparison.OrdinalIgnoreCase);
    }

    private string BuildEmailVerificationLink(string rawToken)
    {
        var baseUrl = _appUrlSettings.PublicBaseUrl?.Trim();
        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new AuthException(500, "App public base URL is not configured.");
        }

        return $"{baseUrl.TrimEnd('/')}/auth/email/verify-link?token={Uri.EscapeDataString(rawToken)}";
    }
}
