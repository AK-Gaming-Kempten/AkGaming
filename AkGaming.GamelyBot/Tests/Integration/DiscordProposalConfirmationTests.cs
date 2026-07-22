using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace AkGaming.GamelyBot.Tests.Integration;

[TestFixture]
public sealed class DiscordProposalConfirmationTests
{
    [Test]
    [Description("Returns an ephemeral confirmation prompt before accepting a rescheduling proposal.")]
    public async Task ProposalDecision_FirstClick_ReturnsConfirmationButtonsWithoutApplyingDecision()
    {
        // Arrange
        var databasePath = Path.Combine(Path.GetTempPath(), $"akgaming-gamelybot-{Guid.NewGuid():N}.db");
        try
        {
            await using var factory = CreateFactory(databasePath);
            using var client = factory.CreateClient();
            var meetingId = Guid.NewGuid();
            var proposalId = Guid.NewGuid();
            var interaction = new
            {
                type = 3,
                guild_id = "test-guild",
                member = new { user = new { id = "discord-user" } },
                data = new { custom_id = $"bp:{meetingId}:{proposalId}:a" }
            };

            // Act
            using var response = await client.PostAsJsonAsync("/api/discord/interactions", interaction);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();

            // Assert
            Assert.That(result.GetProperty("type").GetInt32(), Is.EqualTo(4));
            var data = result.GetProperty("data");
            Assert.That(data.GetProperty("flags").GetInt32(), Is.EqualTo(64));
            var buttons = data.GetProperty("components")[0].GetProperty("components");
            Assert.That(buttons.GetArrayLength(), Is.EqualTo(2));
            Assert.That(buttons[0].GetProperty("custom_id").GetString(),
                Is.EqualTo($"bpc:{meetingId}:{proposalId}:a"));
            Assert.That(buttons[1].GetProperty("custom_id").GetString(), Is.EqualTo("bpx"));
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
        }
    }

    [Test]
    [Description("Replaces an ephemeral proposal confirmation with a final message when it is cancelled.")]
    public async Task ProposalDecision_CancelConfirmation_ReplacesPromptAndRemovesButtons()
    {
        // Arrange
        var databasePath = Path.Combine(Path.GetTempPath(), $"akgaming-gamelybot-{Guid.NewGuid():N}.db");
        try
        {
            await using var factory = CreateFactory(databasePath);
            using var client = factory.CreateClient();
            var interaction = new
            {
                type = 3,
                guild_id = "test-guild",
                member = new { user = new { id = "discord-user" } },
                data = new { custom_id = "bpx" }
            };

            // Act
            using var response = await client.PostAsJsonAsync("/api/discord/interactions", interaction);
            var result = await response.Content.ReadFromJsonAsync<JsonElement>();

            // Assert
            Assert.Multiple(() =>
            {
                Assert.That(result.GetProperty("type").GetInt32(), Is.EqualTo(7));
                Assert.That(result.GetProperty("data").GetProperty("components").GetArrayLength(), Is.Zero);
            });
        }
        finally
        {
            if (File.Exists(databasePath)) File.Delete(databasePath);
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
                    ["Discord:GuildId"] = "test-guild"
                }));
        });
    }
}
