using System.Data;
using AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Management.WebApi.Startup;

public static class WebApplicationExtensions {
    public static IApplicationBuilder UseAppSwagger(this IApplicationBuilder app, IHostEnvironment env) {
        if (env.IsDevelopment()) {
            app.UseWhen(ctx => !ctx.Request.Path.StartsWithSegments("/swagger"), branch => {
                branch.UseAuthentication();
                branch.UseAuthorization();
            });
            app.UseSwagger();
            app.UseSwaggerUI();
        }
        return app;
    }
    public static WebApplication UseDatabaseMigrations(this WebApplication app) {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<MemberManagementDbContext>();
        ResetLegacySqliteDatabaseIfNeeded(app.Environment, db);
        db.Database.Migrate();
        return app;
    }

    public static WebApplication MapDebugEndpoints(this WebApplication app) {
        app.MapGet("/debug/test-auth", [Authorize] () => "ok!");
        app.MapGet("/debug/token", [Authorize] (HttpContext http) => {
            var claims = http.User.Claims.Select(c => new { c.Type, c.Value });
            return Results.Ok(claims);
        });
        return app;
    }

    private static void ResetLegacySqliteDatabaseIfNeeded(IHostEnvironment env, MemberManagementDbContext db) {
        if (!db.Database.IsSqlite() || !(env.IsDevelopment() || env.IsEnvironment("Testing"))) {
            return;
        }

        if (!db.Database.CanConnect()) {
            return;
        }

        var connection = db.Database.GetDbConnection();
        var shouldCloseConnection = connection.State != ConnectionState.Open;

        if (shouldCloseConnection) {
            connection.Open();
        }

        var hasLegacySchemaWithoutHistory = false;

        try {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT name FROM sqlite_master WHERE type = 'table' AND name NOT LIKE 'sqlite_%';";

            using var reader = command.ExecuteReader();
            var tables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            while (reader.Read()) {
                tables.Add(reader.GetString(0));
            }

            hasLegacySchemaWithoutHistory = tables.Count > 0 && !tables.Contains("__EFMigrationsHistory");
        }
        finally {
            if (shouldCloseConnection) {
                connection.Close();
            }
        }

        if (!hasLegacySchemaWithoutHistory) {
            return;
        }

        // Legacy local SQLite databases were previously created via EnsureCreated() and cannot be migrated in place.
        db.Database.EnsureDeleted();
    }
}
