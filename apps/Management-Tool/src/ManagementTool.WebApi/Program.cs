using MemberManagement.Api;
using ManagementTool.WebApi.Startup;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
    builder.Configuration.AddUserSecrets<Program>();
builder.Configuration.AddEnvironmentVariables();

builder.Services
    .AddJsonAndControllers()
    .AddAppSwagger()
    .AddJwtAuthentication(builder.Configuration)
    .AddAppAuthorization()
    .AddMemberManagementModule(builder.Configuration);

var app = builder.Build();

app.UseAppSwagger(app.Environment);

app.UseAuthentication();
app.UseAuthorization();

app.MapMemberManagementEndpoints();
app.MapDebugEndpoints();
app.UseDatabaseMigrations();

app.Run();