using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Frontend.Blazor.Components;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;

using System.Text.Json;
using Frontend.Blazor.ApiClients;
using Frontend.Blazor.Handlers;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddRazorPages();
builder.Services.AddServerSideBlazor();

builder.Services
    .AddAuthentication()
    .AddJwtBearer("keycloak", options =>
    {
        options.Authority = builder.Configuration["Jwt:Authority"];
        options.Audience  = builder.Configuration["Jwt:Audience"];
        options.RequireHttpsMetadata = true;
    });
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = "Cookies";
        options.DefaultChallengeScheme = "oidc";
    })
    .AddCookie("Cookies")
    .AddOpenIdConnect("oidc", options =>
    {
        options.Authority = builder.Configuration["Oidc:Authority"];
        options.ClientId = builder.Configuration["Oidc:ClientId"];
        options.ClientSecret = builder.Configuration["Oidc:ClientSecret"];
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.GetClaimsFromUserInfoEndpoint = true;
        
        options.CallbackPath = builder.Configuration["Oidc:CallbackPath"];
        options.SignedOutCallbackPath = builder.Configuration["Oidc:SignedOutCallbackPath"];

        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.Scope.Add("email");
        options.Scope.Add("offline_access");
    });

builder.Services.AddAuthorization();

builder.Services.AddHttpContextAccessor();

builder.Services.AddTransient<ApiAuthorizationHandler>();

builder.Services.AddHttpClient("ManagementApi", client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!);
    })
    .AddHttpMessageHandler<ApiAuthorizationHandler>();
builder.Services.AddHttpClient<MemberManagementApiClient>(client =>
{
    client.BaseAddress = new Uri(builder.Configuration["Api:BaseUrl"]!);
})
.AddHttpMessageHandler<ApiAuthorizationHandler>();


if (builder.Environment.IsProduction()) {
    builder.Services.AddDataProtection()
        .PersistKeysToFileSystem(new DirectoryInfo("/app/keys"))
        .SetApplicationName("ManagementTool");
}
else {
    builder.Services.AddDataProtection()
        .SetApplicationName("ManagementTool");
}


var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseForwardedHeaders();
app.UseAuthentication();
app.UseAuthorization();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();
app.MapGroup("/api/members")
    .RequireAuthorization();

var auth = app.MapGroup("/authentication");
auth.MapGet("/logout", async (HttpContext context) => {
    var props = new AuthenticationProperties {
        RedirectUri = "/"   // after logout
    };

    await context.SignOutAsync("Cookies", props);
    await context.SignOutAsync("oidc", props);

    return Results.Empty;
});

auth.MapGet("/login", async (HttpContext context) => {
    return Results.Challenge(
        new AuthenticationProperties { RedirectUri = "/" },
        new[] { "oidc" }
    );
});

app.MapGet("/debug/token", [Authorize] async (HttpContext ctx) => {
    var accessToken = await ctx.GetTokenAsync("access_token");
    var idToken     = await ctx.GetTokenAsync("id_token");

    string Decode(string? token) {
        if (token is null) return "(null)";

        var handler = new JwtSecurityTokenHandler();
        var jwt = handler.ReadJwtToken(token);

        return JsonSerializer.Serialize<object>(
            jwt.Payload,
            new JsonSerializerOptions { WriteIndented = true }
        );
    }

    return new {
        accessToken = Decode(accessToken),
        idToken = Decode(idToken)
    };
});


app.Run();