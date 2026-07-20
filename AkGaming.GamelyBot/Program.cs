using System.Security.Claims;
using System.Text.Json.Serialization;
using AkGaming.GamelyBot.Application;
using AkGaming.GamelyBot.Infrastructure;
using AkGaming.GamelyBot.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;

var builder = WebApplication.CreateBuilder(args);
if (builder.Environment.IsDevelopment())
    builder.Configuration.AddUserSecrets<Program>();
builder.Configuration.AddEnvironmentVariables();

var authenticationDisabled = builder.Environment.IsDevelopment() && builder.Configuration.GetValue<bool>("Authentication:Disabled");

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHealthChecks();
builder.Services.Configure<DiscordOptions>(builder.Configuration.GetSection(DiscordOptions.SectionName));
builder.Services.Configure<IdentityClientOptions>(builder.Configuration.GetSection(IdentityClientOptions.SectionName));
builder.Services.Configure<NotificationRoutingOptions>(builder.Configuration.GetSection(NotificationRoutingOptions.SectionName));
builder.Services.PostConfigure<NotificationRoutingOptions>(options =>
{
    if (string.IsNullOrWhiteSpace(options.TreasurerRoleId))
        options.TreasurerRoleId = builder.Configuration[$"{DiscordOptions.SectionName}:TreasurerRoleId"];
});

builder.Services.AddDbContext<GamelyBotDbContext>((serviceProvider, options) =>
{
    var configuration = serviceProvider.GetRequiredService<IConfiguration>();
    var databaseProvider = configuration["Database:Provider"]?.Trim().ToLowerInvariant() ?? "sqlite";
    var connectionString = configuration.GetConnectionString("DefaultConnection") ?? "Data Source=gamelybot.db";
    if (databaseProvider is "postgres" or "postgresql")
        options.UseNpgsql(connectionString, database => database.MigrationsAssembly("AkGaming.GamelyBot.Migrations.Postgres"));
    else if (databaseProvider == "sqlite")
        options.UseSqlite(connectionString, database => database.MigrationsAssembly("AkGaming.GamelyBot.Migrations.Sqlite"));
    else
        throw new InvalidOperationException($"Unsupported database provider '{databaseProvider}'.");
});

if (!authenticationDisabled)
{
    var issuer = builder.Configuration["OpenIddictValidation:Issuer"]
        ?? throw new InvalidOperationException("OpenIddictValidation:Issuer is required when authentication is enabled.");
    builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
    builder.Services.AddOpenIddict().AddValidation(options =>
    {
        options.SetIssuer(new Uri(issuer, UriKind.Absolute));
        options.UseSystemNetHttp();
        options.UseAspNetCore();
    });
}

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("NotificationSubmitter", policy =>
    {
        if (!authenticationDisabled)
        {
            policy.AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
            policy.RequireAuthenticatedUser();
            policy.RequireAssertion(context => HasScope(context.User, "gamelybot_notifications"));
        }
        else
        {
            policy.RequireAssertion(_ => true);
        }
    });
});

builder.Services.AddHttpClient();
builder.Services.AddSingleton<ClientCredentialsTokenProvider>();
builder.Services.AddScoped<INotificationRenderer, NotificationRenderer>();
builder.Services.AddScoped<INotificationInbox, EfNotificationInbox>();
var transport = builder.Configuration["NotificationTransport"]?.Trim().ToLowerInvariant() ?? "debug";
if (transport == "discord")
{
    builder.Services.AddScoped<INotificationTransport, DiscordRestNotificationTransport>();
    builder.Services.AddHostedService<DiscordConfigurationValidator>();
}
else
    builder.Services.AddScoped<INotificationTransport, DebugNotificationTransport>();

var identityBaseUrl = builder.Configuration[$"{IdentityClientOptions.SectionName}:BaseUrl"];
if (builder.Environment.IsDevelopment() && string.IsNullOrWhiteSpace(identityBaseUrl))
    builder.Services.AddScoped<IDiscordLinkResolver, DebugDiscordLinkResolver>();
else
    builder.Services.AddScoped<IDiscordLinkResolver, IdentityDiscordLinkResolver>();
builder.Services.AddHostedService<NotificationDeliveryWorker>();

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<GamelyBotDbContext>();
    await dbContext.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();
app.Run();

static bool HasScope(ClaimsPrincipal principal, string scope)
{
    return principal.Claims
        .Where(claim => claim.Type == "scope")
        .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        .Any(value => string.Equals(value, scope, StringComparison.Ordinal));
}

public partial class Program;
