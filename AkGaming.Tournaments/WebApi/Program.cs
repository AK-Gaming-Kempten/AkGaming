using System.Text.Json.Serialization;
using AkGaming.Tournaments.Application.DependencyInjection;
using AkGaming.Tournaments.Infrastructure.Postgres;
using AkGaming.Tournaments.Infrastructure.Sqlite;
using AkGaming.Tournaments.Infrastructure.Sqlite.Persistence;
using AkGaming.Tournaments.WebApi.Middleware;
using AkGaming.Tournaments.Application.Services;
using AkGaming.Tournaments.WebApi.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddProblemDetails();
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();
builder.Services.AddSwaggerGen();
builder.Services.AddTournamentApplication();
builder.Services.AddScoped<ILogoFileStorage, LogoFileStorage>();

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

if (!string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase) && app.Environment.IsDevelopment())
{
    await app.Services.InitializeTournamentSqliteDatabaseAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.MapOpenApi();
}

app.UseMiddleware<ApiExceptionMiddleware>();
app.UseHttpsRedirection();

app.MapControllers();

app.Run();
