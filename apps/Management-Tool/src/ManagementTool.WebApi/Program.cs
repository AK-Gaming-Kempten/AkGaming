using System.Text.Json.Serialization;
using MemberManagement.Api;
using MemberManagement.Infrastructure.Persistence;
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
    .AddJwtBearer("Bearer", o => {
        o.Authority = builder.Configuration["Jwt:Authority"];
        o.Audience = builder.Configuration["Jwt:Audience"];
        o.RequireHttpsMetadata = true;
        o.TokenValidationParameters = new TokenValidationParameters {
            NameClaimType = "preferred_username",
            RoleClaimType = "roles"
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