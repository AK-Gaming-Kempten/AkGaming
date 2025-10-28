using System.Text.Json.Serialization;
using MemberManagement.Api;
using MemberManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

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

app.Run();