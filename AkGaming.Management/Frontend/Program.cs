using AkGaming.Management.Frontend.Startup;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorAndBlazor();
builder.Services.AddAuthenticationAndAuthorization(builder.Configuration, builder.Environment);
builder.Services.ConfigureForwardedHeaders();
builder.Services.AddHttpClients(builder.Configuration, builder.Environment);
builder.Services.AddDataProtectionForEnvironment(builder.Environment);

var app = builder.Build();

app.ConfigureCultureAndLocalization();
app.ConfigureRequestPipeline();
app.ConfigureAuthenticationEndpoints();
app.ConfigureDebugEndpoints();

app.Run();
// TODO: adjust styles in management (input fields,...) and website (cards, header,..)
