using System.Text;
using System.Security.Claims;
using System.Threading.RateLimiting;
using AkGaming.Identity.Application;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Auth;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Infrastructure;
using AkGaming.Identity.Infrastructure.Persistence;
using AkGaming.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddFixedWindowLimiter("auth", limiter =>
    {
        limiter.PermitLimit = 30;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
    });
});

var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
if (string.IsNullOrWhiteSpace(jwtOptions.SecretKey) || jwtOptions.SecretKey.Length < 32)
{
    throw new InvalidOperationException("Jwt:SecretKey must be configured and at least 32 characters.");
}

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtOptions.Issuer,
            ValidAudience = jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    if (dbContext.Database.IsSqlite())
    {
        await dbContext.Database.EnsureCreatedAsync();
    }
    else
    {
        await dbContext.Database.MigrateAsync();
    }
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapGet("/", () => Results.Redirect("/ui/index.html"));
app.MapGet("/login", () => Results.Redirect("/ui/login.html"));
app.MapGet("/register", () => Results.Redirect("/ui/register.html"));

var auth = app.MapGroup("/auth");
auth.RequireRateLimiting("auth");

auth.MapPost("/register", async (RegisterRequest request, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
{
    try
    {
        var response = await authService.RegisterAsync(request, GetIp(httpContext), cancellationToken);
        return Results.Ok(response);
    }
    catch (AuthException exception)
    {
        return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
    }
});

auth.MapPost("/login", async (LoginRequest request, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
{
    try
    {
        var response = await authService.LoginAsync(request, GetIp(httpContext), cancellationToken);
        return Results.Ok(response);
    }
    catch (AuthException exception)
    {
        return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
    }
});

auth.MapPost("/refresh", async (RefreshRequest request, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
{
    try
    {
        var response = await authService.RefreshAsync(request, GetIp(httpContext), cancellationToken);
        return Results.Ok(response);
    }
    catch (AuthException exception)
    {
        return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
    }
});

auth.MapPost("/logout", async (LogoutRequest request, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
{
    await authService.LogoutAsync(request, GetIp(httpContext), cancellationToken);
    return Results.NoContent();
});

auth.MapPost("/email/send-verification", async (EmailVerificationRequest request, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
{
    try
    {
        var response = await authService.RequestEmailVerificationAsync(request, GetIp(httpContext), cancellationToken);
        return Results.Ok(response);
    }
    catch (AuthException exception)
    {
        return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
    }
});

auth.MapPost("/email/verify", async (VerifyEmailRequest request, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
{
    try
    {
        await authService.VerifyEmailAsync(request, GetIp(httpContext), cancellationToken);
        return Results.NoContent();
    }
    catch (AuthException exception)
    {
        return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
    }
});

auth.MapGet("/discord/start", async (IAuthService authService, CancellationToken cancellationToken) =>
{
    var response = await authService.GetDiscordStartUrlAsync(cancellationToken);
    return Results.Redirect(response.AuthorizationUrl);
});

auth.MapGet("/discord/callback", async (string code, string state, IAuthService authService, HttpContext httpContext, CancellationToken cancellationToken) =>
{
    try
    {
        var response = await authService.HandleDiscordCallbackAsync(code, state, GetIp(httpContext), cancellationToken);
        var fragment = BuildDiscordCallbackFragment(
            success: true,
            message: response.Message,
            accessToken: response.Tokens?.AccessToken,
            refreshToken: response.Tokens?.RefreshToken,
            linked: response.Linked,
            createdUser: response.CreatedUser);

        return Results.Redirect($"/ui/callback.html#{fragment}");
    }
    catch (AuthException exception)
    {
        var fragment = BuildDiscordCallbackFragment(
            success: false,
            message: exception.Message,
            errorCode: exception.StatusCode.ToString());

        return Results.Redirect($"/ui/callback.html#{fragment}");
    }
});

auth.MapPost("/discord/link", [Authorize] async (ClaimsPrincipal user, IAuthService authService, CancellationToken cancellationToken) =>
{
    var userIdClaim = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? user.FindFirstValue("sub");
    if (!Guid.TryParse(userIdClaim, out var userId))
    {
        return Results.Unauthorized();
    }

    var response = await authService.GetDiscordLinkUrlAsync(userId, cancellationToken);
    return Results.Ok(response);
});

app.Run();

static string? GetIp(HttpContext context)
{
    return context.Connection.RemoteIpAddress?.ToString();
}

static string BuildDiscordCallbackFragment(
    bool success,
    string message,
    string? accessToken = null,
    string? refreshToken = null,
    bool? linked = null,
    bool? createdUser = null,
    string? errorCode = null)
{
    var parts = new List<string>
    {
        $"success={(success ? "1" : "0")}",
        $"message={Uri.EscapeDataString(message)}"
    };

    if (!string.IsNullOrWhiteSpace(accessToken))
    {
        parts.Add($"accessToken={Uri.EscapeDataString(accessToken)}");
    }

    if (!string.IsNullOrWhiteSpace(refreshToken))
    {
        parts.Add($"refreshToken={Uri.EscapeDataString(refreshToken)}");
    }

    if (linked.HasValue)
    {
        parts.Add($"linked={(linked.Value ? "1" : "0")}");
    }

    if (createdUser.HasValue)
    {
        parts.Add($"createdUser={(createdUser.Value ? "1" : "0")}");
    }

    if (!string.IsNullOrWhiteSpace(errorCode))
    {
        parts.Add($"errorCode={Uri.EscapeDataString(errorCode)}");
    }

    return string.Join("&", parts);
}

public partial class Program;
