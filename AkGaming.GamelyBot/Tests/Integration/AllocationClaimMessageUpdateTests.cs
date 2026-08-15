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

    [Test]
    [Description("Creates a claim message in the new destination when an existing allocation's Discord channel changes.")]
    public async Task ClaimSnapshots_WhenChannelChanges_CreatesMessageInNewChannel()
    {
        // Arrange
        var databasePath = Path.Combine(Path.GetTempPath(), $"akgaming-gamelybot-{Guid.NewGuid():N}.db");
        try
        {
            await using var factory = CreateFactory(databasePath);
            using var client = factory.CreateClient();
            var applicationId = Guid.NewGuid();
            var first = Envelope(applicationId, ["Anna"], [], "channel-123");
            var second = Envelope(applicationId, ["Anna"], [], "channel-456");

            // Act
            await client.PostAsJsonAsync("/api/notifications", first);
            await WaitForDeliveriesAsync(client, 1);
            await client.PostAsJsonAsync("/api/notifications", second);
            var deliveries = await WaitForDeliveriesAsync(client, 2);

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(deliveries.EnumerateArray()
                    .Select(item => item.GetProperty("target").GetString()),
                    Is.EquivalentTo(new[] { "channel-123", "channel-456" }));
                Assert.That(deliveries.EnumerateArray()
                    .Select(item => item.GetProperty("externalMessageId").GetString())
                    .Distinct()
                    .ToList(), Has.Count.EqualTo(2));
            });
        }
        finally
        {
            if (File.Exists(databasePath))
                File.Delete(databasePath);
        }
    }

    [Test]
    [Description("Creates a new Discord claim message when an amount change starts a fresh approval review.")]
    public async Task ClaimSnapshots_WhenNewReviewStarts_CreatesNewDiscordMessage()
    {
        // Arrange
        var databasePath = Path.Combine(Path.GetTempPath(), $"akgaming-gamelybot-{Guid.NewGuid():N}.db");
        try
        {
            await using var factory = CreateFactory(databasePath);
            using var client = factory.CreateClient();
            var applicationId = Guid.NewGuid();
            var original = Envelope(applicationId, ["Anna"], []);
            var amountChanged = Envelope(applicationId, [], [], startsNewReview: true);
            var newApproval = Envelope(applicationId, ["Berta"], []);

            // Act
            await client.PostAsJsonAsync("/api/notifications", original);
            var firstDelivery = await WaitForDeliveriesAsync(client, 1);
            await client.PostAsJsonAsync("/api/notifications", amountChanged);
            var newReviewDeliveries = await WaitForDeliveriesAsync(client, 2);
            await client.PostAsJsonAsync("/api/notifications", newApproval);
            var deliveries = await WaitForDeliveriesAsync(client, 3);

            // Assert
            var originalMessageId = firstDelivery[0].GetProperty("externalMessageId").GetString();
            var newReviewMessageId = newReviewDeliveries.EnumerateArray()
                .Select(item => item.GetProperty("externalMessageId").GetString())
                .Single(id => id != originalMessageId);
            var messageIds = deliveries.EnumerateArray()
                .Select(item => item.GetProperty("externalMessageId").GetString())
                .ToList();
            Assert.Multiple(() =>
            {
                Assert.That(messageIds.Distinct().ToList(), Has.Count.EqualTo(2));
                Assert.That(messageIds.Count(id => id == originalMessageId), Is.EqualTo(1));
                Assert.That(messageIds.Count(id => id == newReviewMessageId), Is.EqualTo(2));
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
        IReadOnlyList<string> objections,
        string channelId = "channel-123",
        bool startsNewReview = false)
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
            channelId,
            "role-456",
            startsNewReview);
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
