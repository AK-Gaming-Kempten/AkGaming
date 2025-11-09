using MemberManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace ManagementTool.WebApi.Startup;

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
}
