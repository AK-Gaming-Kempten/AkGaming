using System.Text;
using System.Security.Claims;
using AkGaming.Identity.Application;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Application.Auth;
using AkGaming.Identity.Application.Common;
using AkGaming.Identity.Infrastructure;
using AkGaming.Identity.Infrastructure.Persistence;
using AkGaming.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

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
app.UseAuthentication();
app.UseAuthorization();

var auth = app.MapGroup("/auth");

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
        return Results.Ok(response);
    }
    catch (AuthException exception)
    {
        return Results.Problem(statusCode: exception.StatusCode, detail: exception.Message);
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

public partial class Program;
