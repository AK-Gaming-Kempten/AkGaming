using System.Text;
using System.Threading.RateLimiting;
using AkGaming.Identity.Application;
using AkGaming.Identity.Api.Endpoints;
using AkGaming.Identity.Infrastructure;
using AkGaming.Identity.Infrastructure.OpenIddict;
using AkGaming.Identity.Infrastructure.Persistence;
using AkGaming.Identity.Infrastructure.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;
using System.Security.Cryptography.X509Certificates;

var builder = WebApplication.CreateBuilder(args);
var jwtOptions = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
var openIddictSeedOptions = builder.Configuration.GetSection(OpenIddictSeedOptions.SectionName).Get<OpenIddictSeedOptions>() ?? new OpenIddictSeedOptions();
var openIddictCredentialOptions = builder.Configuration.GetSection(OpenIddictCredentialOptions.SectionName).Get<OpenIddictCredentialOptions>() ?? new OpenIddictCredentialOptions();
var issuer = openIddictSeedOptions.Issuer ?? builder.Configuration["App:PublicBaseUrl"] ?? "https://localhost:5001";
var registeredScopes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
{
    OpenIddictConstants.Scopes.OpenId,
    OpenIddictConstants.Scopes.Profile,
    OpenIddictConstants.Scopes.Email,
    "roles"
};

foreach (var scope in openIddictSeedOptions.Scopes.Where(x => !string.IsNullOrWhiteSpace(x.Name)))
{
    registeredScopes.Add(scope.Name);
}

builder.Services.AddOpenApi();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddControllersWithViews();
builder.Services.AddRazorPages();
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
});
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

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
    {
        options.LoginPath = "/account/login";
        options.LogoutPath = "/account/logout";
        options.AccessDeniedPath = "/account/access-denied";
        options.Cookie.Name = "akgaming.identity";
        options.SlidingExpiration = true;
    });

builder.Services.AddOpenIddict()
    .AddCore(options =>
    {
        options.UseEntityFrameworkCore()
            .UseDbContext<AuthDbContext>();
    })
    .AddServer(options =>
    {
        options.SetIssuer(new Uri(issuer, UriKind.Absolute));
        options.SetAuthorizationEndpointUris("/connect/authorize");
        options.SetTokenEndpointUris("/connect/token");
        options.SetUserInfoEndpointUris("/connect/userinfo");
        options.SetEndSessionEndpointUris("/connect/logout");

        options.AllowAuthorizationCodeFlow();
        options.AllowRefreshTokenFlow();
        options.RequireProofKeyForCodeExchange();
        options.DisableAccessTokenEncryption();

        options.SetAccessTokenLifetime(TimeSpan.FromMinutes(jwtOptions.AccessTokenMinutes));
        options.SetRefreshTokenLifetime(TimeSpan.FromDays(jwtOptions.RefreshTokenDays));
        options.RegisterScopes([.. registeredScopes]);

        if (builder.Environment.IsDevelopment() || builder.Environment.IsEnvironment("Testing"))
        {
            options.AddDevelopmentEncryptionCertificate()
                .AddDevelopmentSigningCertificate();
        }
        else
        {
            var signingCertificate = LoadCertificate(openIddictCredentialOptions.Signing, builder.Environment.ContentRootPath, "signing");
            var encryptionCertificate = LoadCertificate(openIddictCredentialOptions.Encryption, builder.Environment.ContentRootPath, "encryption");

            options.AddSigningCertificate(signingCertificate)
                .AddEncryptionCertificate(encryptionCertificate);
        }

        options.UseAspNetCore()
            .EnableAuthorizationEndpointPassthrough()
            .EnableTokenEndpointPassthrough()
            .EnableUserInfoEndpointPassthrough()
            .EnableEndSessionEndpointPassthrough()
            .EnableStatusCodePagesIntegration();
    })
    .AddValidation(options =>
    {
        options.UseLocalServer();
        options.UseAspNetCore();
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApiAccess", policy =>
    {
        policy.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
        policy.RequireAuthenticatedUser();
    });
});

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
    await dbContext.Database.MigrateAsync();
    var openIddictSeeder = scope.ServiceProvider.GetRequiredService<OpenIddictSeeder>();
    await openIddictSeeder.SeedAsync(CancellationToken.None);
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseForwardedHeaders();

if (!app.Environment.IsEnvironment("Testing"))
{
    app.UseHttpsRedirection();
}
app.UseDefaultFiles();
app.UseStaticFiles();
app.UseStatusCodePagesWithReExecute("/error");
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapRazorPages();
app.MapGet("/", () => Results.Redirect("/account/manage"));
app.MapGet("/login", (HttpContext context) => Results.Redirect($"/account/login{context.Request.QueryString}"));
app.MapGet("/register", (HttpContext context) => Results.Redirect($"/account/register{context.Request.QueryString}"));
app.MapAuthEndpoints();
app.MapAdminEndpoints();

app.Run();

static X509Certificate2 LoadCertificate(OpenIddictCertificateOptions options, string contentRootPath, string purpose)
{
    if (string.IsNullOrWhiteSpace(options.Path))
    {
        throw new InvalidOperationException($"OpenIddict {purpose} certificate path is required outside development/testing.");
    }

    var fullPath = Path.IsPathRooted(options.Path)
        ? options.Path
        : Path.Combine(contentRootPath, options.Path);

    if (!File.Exists(fullPath))
    {
        throw new InvalidOperationException($"OpenIddict {purpose} certificate file was not found: {fullPath}");
    }

    return X509CertificateLoader.LoadPkcs12FromFile(
        fullPath,
        options.Password,
        X509KeyStorageFlags.MachineKeySet | X509KeyStorageFlags.Exportable,
        Pkcs12LoaderLimits.Defaults);
}

public partial class Program;
