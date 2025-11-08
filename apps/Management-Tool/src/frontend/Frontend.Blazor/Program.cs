using Frontend.Blazor.Startup;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorAndBlazor();
builder.Services.AddAuthenticationAndAuthorization(builder.Configuration);
builder.Services.ConfigureForwardedHeaders();
builder.Services.AddHttpClients(builder.Configuration);
builder.Services.AddDataProtectionForEnvironment(builder.Environment);

var app = builder.Build();

app.ConfigureRequestPipeline();
app.ConfigureAuthenticationEndpoints();
app.ConfigureDebugEndpoints();

app.Run();
