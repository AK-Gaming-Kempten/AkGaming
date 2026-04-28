using AkGaming.Tournaments.Frontend.Startup;

var builder = WebApplication.CreateBuilder(args);
builder.WebHost.UseStaticWebAssets();

builder.Services.AddRazorAndBlazor();
builder.Services.AddAuthenticationAndAuthorization(builder.Configuration, builder.Environment);
builder.Services.ConfigureForwardedHeaders();
builder.Services.AddTournamentApiClients(builder.Configuration);
builder.Services.AddDataProtectionForEnvironment(builder.Configuration, builder.Environment);

var app = builder.Build();

app.ConfigureCultureAndLocalization();
app.ConfigureRequestPipeline();
app.ConfigureAuthenticationEndpoints();

app.Run();
