using System.Net.Http.Json;
using System.Text.Json;
using AkGaming.Core.Notifications;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AkGaming.GamelyBot.Tests.Integration;

[TestFixture]
public sealed class DebugNotificationFlowTests
{
    [Test]
    [Description("Accepts, stores, renders, and captures channel and direct messages without contacting Discord.")]
    public async Task NotificationFlow_InDebugMode_CapturesRenderedDeliveries()
    {
        // Arrange
        var databasePath = Path.Combine(Path.GetTempPath(), $"akgaming-gamelybot-{Guid.NewGuid():N}.db");
        try
        {
            await using var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
            {
                builder.UseEnvironment("Development");
                builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = $"Data Source={databasePath}",
                    ["Database:Provider"] = "sqlite",
                    ["Authentication:Disabled"] = "true",
                    ["NotificationTransport"] = "debug",
                    ["IdentityClient:BaseUrl"] = "",
                    ["IdentityClient:DebugDiscordUserId"] = "linked-debug-user"
                }));
            });
            using var client = factory.CreateClient();
            var data = new ReimbursementSubmittedNotification(Guid.NewGuid(), "Applicant", "Travel", 42.50m, "Submitted", null);
            var request = new NotificationEnvelope(
                Guid.NewGuid(),
                NotificationEventTypes.ReimbursementSubmitted,
                "management",
                DateTimeOffset.UtcNow,
                Guid.NewGuid(),
                JsonSerializer.SerializeToElement(data, new JsonSerializerOptions(JsonSerializerDefaults.Web)));

            // Act
            var response = await client.PostAsJsonAsync("/api/notifications", request);
            JsonElement deliveries = default;
            for (var attempt = 0; attempt < 20; attempt++)
            {
                await Task.Delay(150);
                deliveries = await client.GetFromJsonAsync<JsonElement>("/api/debug/deliveries");
                if (deliveries.ValueKind == JsonValueKind.Array && deliveries.GetArrayLength() == 2)
                    break;
            }

            // Assert
            Assert.That(response.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Accepted));
            Assert.That(deliveries.GetArrayLength(), Is.EqualTo(2));
            Assert.That(deliveries.EnumerateArray().Select(item => item.GetProperty("kind").GetString()),
                Is.EquivalentTo(new[] { "channel", "direct-message" }));
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }
}
