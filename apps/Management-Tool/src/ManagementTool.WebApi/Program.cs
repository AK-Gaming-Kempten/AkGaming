using System.Text.Json.Serialization;
using MemberManagement.Api;

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

if (app.Environment.IsDevelopment()) {
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.Run();