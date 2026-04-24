using System.Text.Json.Serialization;
using AkGaming.Tournaments.Application.DependencyInjection;
using AkGaming.Tournaments.Infrastructure.Postgres;
using AkGaming.Tournaments.Infrastructure.Sqlite;
using AkGaming.Tournaments.WebApi.Endpoints;
using AkGaming.Tournaments.WebApi.Middleware;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});
builder.Services.AddTournamentApplication();

var provider = builder.Configuration["Persistence:Provider"];
if (string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase))
{
    builder.Services.AddTournamentPostgresInfrastructure(builder.Configuration);
}
else
{
    builder.Services.AddTournamentSqliteInfrastructure(builder.Configuration);
}

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseHttpsRedirection();

app.MapGameEndpoints();
app.MapPlayerProfileEndpoints();
app.MapTeamEndpoints();
app.MapTournamentRegistrationEndpoints();

app.Run();
