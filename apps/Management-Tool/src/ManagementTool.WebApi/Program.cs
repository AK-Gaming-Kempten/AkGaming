using System.Security.Claims;
using System.Text.Json.Serialization;
using MemberManagement.Api;
using MemberManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options => {
    options.SchemaGeneratorOptions = new() {
        UseInlineDefinitionsForEnums = true
    };
});

builder.Services
    .AddAuthentication("Bearer")
    .AddJwtBearer("Bearer", option => {
        option.Authority = builder.Configuration["Jwt:Authority"];
        option.Audience = builder.Configuration["Jwt:Audience"];
        option.RequireHttpsMetadata = true;
        option.TokenValidationParameters = new TokenValidationParameters {
            NameClaimType = "preferred_username",
            RoleClaimType = ClaimTypes.Role
        };
        option.Events = new JwtBearerEvents {
            OnTokenValidated = context => {
                var claims = context.Principal!.Claims.ToList();
                var identity = (ClaimsIdentity)context.Principal.Identity!;

                // Extract roles from realm_access.roles[]
                var realmRoles = context.Principal.FindAll("realm_access.roles");
                foreach (var r in realmRoles)
                    identity.AddClaim(new Claim(ClaimTypes.Role, r.Value));

                // Extract roles from resource_access.<client>.roles[]
                var resourceRoles = context.Principal.FindAll("resource_access.managementtool-api.roles");
                foreach (var r in resourceRoles)
                    identity.AddClaim(new Claim(ClaimTypes.Role, r.Value));

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("UserOnly", p => p.RequireRole("User", "Admin"));
    options.AddPolicy("MemberOnly", p => p.RequireRole("Member", "Admin"));
    options.AddPolicy("AdminOnly",  p => p.RequireRole("Admin"));
});


builder.Services.AddMemberManagementModule(builder.Configuration);

var app = builder.Build();
app.MapMemberManagementEndpoints();
app.MapGet("/test-auth", [Authorize] () => "ok!");
app.MapGet("/debug/claims", [Authorize] (HttpContext http) =>
{
    var claims = http.User.Claims
        .Select(c => new { c.Type, c.Value })
        .ToList();

    return Results.Ok(claims);
});

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<MemberManagementDbContext>();
    db.Database.Migrate();
}

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.Run();