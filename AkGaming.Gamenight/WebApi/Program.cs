using AkGaming.Gamenight.Application;
using AkGaming.Core.Authentication;
using AkGaming.Gamenight.Contracts;
using AkGaming.Gamenight.Infrastructure;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using OpenIddict.Validation.AspNetCore;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddHealthChecks();
builder.Services.AddAuthentication(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme);
builder.Services.AddOpenIddict().AddValidation(options =>
{
    options.SetIssuer(new Uri(builder.Configuration["Identity:Authority"]!));
    options.AddAudiences("gamenight_api");
    options.UseSystemNetHttp(http => http.ConfigureHttpClientHandler(handler =>
        DevelopmentHttpCertificates.Configure(handler, builder.Environment, builder.Configuration)));
    options.UseAspNetCore();
});
builder.Services.AddAuthorization(options =>
{
    options.DefaultPolicy = new Microsoft.AspNetCore.Authorization.AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser().RequireAssertion(c => HasScope(c.User)).Build();
    foreach (var permission in Permissions.All)
        options.AddPolicy(permission, p => p.RequireAuthenticatedUser().RequireAssertion(c => HasScope(c.User)).RequireClaim("permission", permission));
});
builder.Services.AddDbContext<GamenightDbContext>(options =>
{
    var connection = builder.Configuration.GetConnectionString("Gamenight")!;
    if (builder.Configuration["Database:Provider"] == "Postgres")
        options.UseNpgsql(connection, p => p.MigrationsAssembly("AkGaming.Gamenight.Migrations.Postgres"));
    else options.UseSqlite(connection, p => p.MigrationsAssembly("AkGaming.Gamenight.Migrations.Sqlite"));
});
builder.Services.AddScoped<IGamenightStore>(s => s.GetRequiredService<GamenightDbContext>());
builder.Services.AddScoped<IGamenightService, GamenightService>();
builder.Services.AddScoped<IMembershipClient, ManagementMembershipClient>();
builder.Services.AddScoped<IGuestEmail, GuestEmail>();
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient(Microsoft.Extensions.Options.Options.DefaultName, c => c.Timeout = TimeSpan.FromSeconds(10))
    .ConfigurePrimaryHttpMessageHandler(() => DevelopmentHttpCertificates.CreateHandler(builder.Environment, builder.Configuration));
builder.Services.AddScoped<ServiceTokenClient>();
builder.Services.AddScoped<GamelyBotConnection>();
builder.Services.AddHostedService<EmailDispatcher>();
builder.Services.AddDataProtection().SetApplicationName("AkGaming.Gamenight.Api")
    .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:KeyDirectory"] ?? "/tmp/akgaming-gamenight-api-keys"));
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = 429;
    options.AddPolicy("guest", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions { PermitLimit = 20, Window = TimeSpan.FromMinutes(1), QueueLimit = 0 }));
});
var app = builder.Build();
app.UseExceptionHandler(handler => handler.Run(async context =>
{
    var error = context.Features.Get<Microsoft.AspNetCore.Diagnostics.IExceptionHandlerFeature>()?.Error;
    var status = error is GamenightException domain ? domain.Status : 500;
    context.Response.StatusCode = status;
    await context.Response.WriteAsJsonAsync(new Microsoft.AspNetCore.Mvc.ProblemDetails
    {
        Status = status, Title = error is GamenightException ? error.Message : "Die Anfrage konnte nicht verarbeitet werden."
    });
}));
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<GamenightDbContext>().Database.MigrateAsync();
}
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();
app.Run();

static bool HasScope(System.Security.Claims.ClaimsPrincipal user) =>
    user.FindAll("scope").SelectMany(c => c.Value.Split(' ')).Contains("gamenight_api");
public partial class Program { }
