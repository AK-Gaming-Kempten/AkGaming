using System.Net.Http.Json;
using System.Text.Json;
using AkGaming.Core.Notifications;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AkGaming.GamelyBot.Tests.Integration;

[TestFixture]
public sealed class AllocationClaimMessageUpdateTests
{
    [Test]
    [Description("Updates the existing Discord claim message when approvals or objections change.")]
    public async Task ClaimSnapshots_WhenApplicationMatches_ReuseExistingDiscordMessage()
    {
        // Arrange
        var databasePath = Path.Combine(Path.GetTempPath(), $"akgaming-gamelybot-{Guid.NewGuid():N}.db");
        try
        {
            await using var factory = CreateFactory(databasePath);
            using var client = factory.CreateClient();
            var applicationId = Guid.NewGuid();
            var first = Envelope(applicationId, ["Anna"], []);
            var second = Envelope(applicationId, ["Anna"], ["Berta"]);

            // Act
            var firstResponse = await client.PostAsJsonAsync("/api/notifications", first);
            var firstDeliveries = await WaitForDeliveriesAsync(client, 1);
            var secondResponse = await client.PostAsJsonAsync("/api/notifications", second);
            var allDeliveries = await WaitForDeliveriesAsync(client, 2);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(firstResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Accepted));
                Assert.That(secondResponse.StatusCode, Is.EqualTo(System.Net.HttpStatusCode.Accepted));
                Assert.That(firstDeliveries.GetArrayLength(), Is.EqualTo(1));
                Assert.That(allDeliveries.GetArrayLength(), Is.EqualTo(2));
                Assert.That(allDeliveries.EnumerateArray()
                    .Select(item => item.GetProperty("externalMessageId").GetString())
                    .Distinct()
                    .ToList(), Has.Count.EqualTo(1));
                Assert.That(allDeliveries.EnumerateArray()
                    .Select(item => item.GetProperty("body").GetString()), Has.Some.Contains("Berta"));
            });
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    private static WebApplicationFactory<Program> CreateFactory(string databasePath)
    {
        return new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseEnvironment("Development");
            builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:DefaultConnection"] = $"Data Source={databasePath}",
                    ["Database:Provider"] = "sqlite",
                    ["Authentication:Disabled"] = "true",
                    ["NotificationTransport"] = "debug",
                    ["IdentityClient:BaseUrl"] = "",
                    ["IdentityClient:DebugDiscordUserId"] = "linked-debug-user"
                }));
        });
    }

    private static NotificationEnvelope Envelope(
        Guid applicationId,
        IReadOnlyList<string> approvals,
        IReadOnlyList<string> objections)
    {
        var data = new AllocationClaimChangedNotification(
            applicationId,
            "Summer cup",
            "Team prize",
            "Chris",
            200m,
            null,
            "Submitted",
            approvals,
            objections,
            "https://management.test/claim",
            "channel-123",
            "role-456");
        return new NotificationEnvelope(
            Guid.NewGuid(),
            NotificationEventTypes.AllocationClaimChanged,
            "management",
            DateTimeOffset.UtcNow,
            Guid.NewGuid(),
            JsonSerializer.SerializeToElement(data, new JsonSerializerOptions(JsonSerializerDefaults.Web)));
    }

    private static async Task<JsonElement> WaitForDeliveriesAsync(HttpClient client, int expectedCount)
    {
        JsonElement deliveries = default;
        for (var attempt = 0; attempt < 30; attempt++)
        {
            await Task.Delay(150);
            deliveries = await client.GetFromJsonAsync<JsonElement>("/api/debug/deliveries");
            if (deliveries.ValueKind == JsonValueKind.Array
                && deliveries.GetArrayLength() == expectedCount
                && deliveries.EnumerateArray().All(item => item.GetProperty("status").GetString() == "delivered"))
                return deliveries;
        }

        return deliveries;
    }
}
