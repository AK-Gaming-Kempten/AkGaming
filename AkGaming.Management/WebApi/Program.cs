using AkGaming.Management.Modules.MemberManagement.Api;
using AkGaming.Management.Modules.InvoiceManagement.Api;
using AkGaming.Management.Modules.Disbursements.Api;
using AkGaming.Management.Modules.GeneralMeetings.Api;
using AkGaming.Management.WebApi.Startup;

var builder = WebApplication.CreateBuilder(args);

if (builder.Environment.IsDevelopment())
    builder.Configuration.AddUserSecrets<Program>();
builder.Configuration.AddEnvironmentVariables();

builder.Services.AddHealthChecks();
builder.Services
    .AddJsonAndControllers()
    .AddAppSwagger()
    .AddOpenIddictAuthentication(builder.Configuration, builder.Environment)
    .AddAppAuthorization()
    .AddMemberManagementModule(builder.Configuration)
    .AddInvoiceManagementModule(builder.Configuration)
    .AddDisbursementsModule(builder.Configuration)
    .AddGeneralMeetingsModule(builder.Configuration);

var app = builder.Build();

app.UseAppSwagger(app.Environment);

app.UseAuthentication();
app.UseAuthorization();

app.MapMemberManagementEndpoints();
app.MapControllers();
app.MapHealthChecks("/health").AllowAnonymous();
app.MapGeneralMeetingsHub();
app.MapDebugEndpoints();
app.UseDatabaseMigrations();

app.Run();
