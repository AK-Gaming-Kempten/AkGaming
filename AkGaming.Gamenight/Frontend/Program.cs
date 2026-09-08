using AkGaming.Core.Components.Authentication;
using AkGaming.Core.Authentication;
using AkGaming.Gamenight.Frontend;
using AkGaming.Gamenight.Shared;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Caching.Distributed;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddControllers();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHealthChecks();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSingleton<ITicketStore>(s => new DistributedCacheTicketStore(s.GetRequiredService<IDistributedCache>(), "akgaming.gamenight.ticket"));
builder.Services.AddScoped<AuthenticationTicketTokenUpdater>();
builder.Services.AddDataProtection().SetApplicationName("AkGaming.Gamenight.Web")
    .PersistKeysToFileSystem(new DirectoryInfo(builder.Configuration["DataProtection:KeyDirectory"] ?? "/tmp/akgaming-gamenight-web-keys"));
builder.Services.AddAuthentication(options =>
{
    options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
})
.AddCookie(options => { options.Cookie.Name = "akgaming.gamenight"; options.Cookie.SecurePolicy = CookieSecurePolicy.Always; options.ExpireTimeSpan = TimeSpan.FromDays(7); })
.AddOpenIdConnect(options =>
{
    options.Authority = builder.Configuration["Identity:Authority"];
    options.BackchannelHttpHandler = DevelopmentHttpCertificates.CreateHandler(builder.Environment, builder.Configuration);
    options.ClientId = "akgaming-gamenight-web";
    options.ClientSecret = builder.Configuration["Identity:ClientSecret"];
    options.ResponseType = "code"; options.UsePkce = true; options.SaveTokens = true; options.MapInboundClaims = false;
    options.PushedAuthorizationBehavior = PushedAuthorizationBehavior.Disable;
    options.Scope.Clear();
    foreach (var scope in new[] { "openid", "profile", "email", "roles", "offline_access", "gamenight_api" }) options.Scope.Add(scope);
    options.TokenValidationParameters.NameClaimType = "email";
});
builder.Services.AddOptions<CookieAuthenticationOptions>(CookieAuthenticationDefaults.AuthenticationScheme)
    .Configure<ITicketStore>((options, store) => options.SessionStore = store);
builder.Services.AddAuthorization();
builder.Services.AddHttpClient(Microsoft.Extensions.Options.Options.DefaultName)
    .ConfigurePrimaryHttpMessageHandler(() => DevelopmentHttpCertificates.CreateHandler(builder.Environment, builder.Configuration));
builder.Services.AddHttpClient("GamenightApi", client => client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!))
    .ConfigurePrimaryHttpMessageHandler(() => DevelopmentHttpCertificates.CreateHandler(builder.Environment, builder.Configuration));
builder.Services.AddScoped<IUserSession, WebSession>();
builder.Services.AddScoped<IRegistrationExport, BrowserRegistrationExport>();
builder.Services.AddScoped(s => new GamenightClient(s.GetRequiredService<IHttpClientFactory>().CreateClient("GamenightApi"), s.GetRequiredService<IUserSession>()));
var app = builder.Build();
if (!app.Environment.IsDevelopment()) { app.UseExceptionHandler("/error"); app.UseHsts(); }
app.UseHttpsRedirection();
app.Use(async (context, next) =>
{
    context.Response.Headers["Referrer-Policy"] = "no-referrer";
    context.Response.Headers.CacheControl = "no-store";
    await next();
});
app.UseAuthentication(); app.UseAuthorization(); app.UseAntiforgery();
app.MapStaticAssets();
app.MapControllers();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode().AddAdditionalAssemblies(typeof(Routes).Assembly);
app.MapHealthChecks("/health").AllowAnonymous();
app.Run();
