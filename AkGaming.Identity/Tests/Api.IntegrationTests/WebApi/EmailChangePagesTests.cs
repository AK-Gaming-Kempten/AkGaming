using System.Collections.Concurrent;
using System.ComponentModel;
using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using AkGaming.Core.Common.Email;
using AkGaming.Identity.Application.Abstractions;
using AkGaming.Identity.Domain.Entities;
using AkGaming.Identity.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AkGaming.Identity.Api.IntegrationTests.WebApi;

public sealed class EmailChangePagesFixture : IDisposable
{
    private TestApiFactory RootFactory { get; } = new();
    internal WebApplicationFactory<Program> Factory { get; }
    internal RecordingEmailSender Sender { get; } = new();

    public EmailChangePagesFixture()
    {
        Factory = RootFactory.WithWebHostBuilder(builder => builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IEmailSender>();
            services.AddSingleton<IEmailSender>(Sender);
        }));
    }

    public void Dispose()
    {
        Factory.Dispose();
        RootFactory.Dispose();
    }
}

internal sealed class RecordingEmailSender : IEmailSender
{
    internal ConcurrentQueue<(string To, string Text)> Messages { get; } = new();
    public Task SendAsync(string toEmail, string subject, string textBody, string? htmlBody, CancellationToken cancellationToken)
    {
        Messages.Enqueue((toEmail, textBody));
        return Task.CompletedTask;
    }
}

public sealed class EmailChangePagesTests : IClassFixture<EmailChangePagesFixture>, IDisposable
{
    private EmailChangePagesFixture Fixture { get; }
    private HttpClient Client { get; }
    private User Account { get; } = new() { Email = $"page-{Guid.NewGuid():N}@example.com", Username = "Email Test", IsEmailVerified = true };
    private string NewEmail { get; } = $"new-{Guid.NewGuid():N}@example.com";
    private const string Password = "Password123";

    public EmailChangePagesTests(EmailChangePagesFixture fixture)
    {
        Fixture = fixture;
        Client = CreateClient();
    }

    [Fact]
    [Description("The account flow preserves the original email until an antiforgery-protected confirmation POST, then expires existing local sessions and accepts the new login address.")]
    public async Task ChangeEmail_CompletesThroughConfirmationPage()
    {
        // Arrange
        await SeedAndLoginAsync();
        var issue = await PostFormAsync(Client, "/account/manage", "/account/manage?handler=RequestEmailChange",
            new() { ["NewEmail"] = NewEmail, ["CurrentPassword"] = Password });
        Assert.Equal(HttpStatusCode.Redirect, issue.StatusCode);
        var token = GetConfirmationToken();
        using var mailboxClient = CreateClient();
        var confirmationUrl = $"/account/email-change?token={Uri.EscapeDataString(token)}";

        // Act
        var page = await mailboxClient.GetAsync(confirmationUrl);
        var before = await ReadAccountAsync();
        var unprotected = await mailboxClient.PostAsync("/account/email-change", new FormUrlEncodedContent(new Dictionary<string, string> { ["Token"] = token }));
        var response = await PostFormAsync(mailboxClient, confirmationUrl, "/account/email-change", new() { ["Token"] = token });
        var oldSession = await Client.GetAsync("/account/manage");
        var after = await ReadAccountAsync();
        await LoginAsync(mailboxClient, NewEmail);
        var newProfile = await mailboxClient.GetStringAsync("/account/manage");

        // Assert
        Assert.Equal(HttpStatusCode.OK, page.StatusCode);
        Assert.Equal("no-referrer", page.Headers.GetValues("Referrer-Policy").Single());
        Assert.Equal(Account.Email, before.Email);
        Assert.Equal(HttpStatusCode.BadRequest, unprotected.StatusCode);
        Assert.Contains("Your email address has changed", await response.Content.ReadAsStringAsync());
        Assert.Equal(HttpStatusCode.Redirect, oldSession.StatusCode);
        Assert.Equal("/account/login", new Uri(new Uri("https://localhost"), oldSession.Headers.Location!).AbsolutePath);
        Assert.Equal(NewEmail, after.Email);
        Assert.Equal(Account.Id, after.Id);
        Assert.True(after.IsEmailVerified);
        Assert.Contains(NewEmail, newProfile);
        Assert.Contains(Fixture.Sender.Messages, message => message.To == Account.Email && message.Text.Contains(NewEmail));
    }

    [Fact]
    [Description("The pending change appears in the account page and its cancellation action invalidates the emailed token.")]
    public async Task Cancel_RemovesPendingChange_AndRejectsLink()
    {
        // Arrange
        await SeedAndLoginAsync();
        await PostFormAsync(Client, "/account/manage", "/account/manage?handler=RequestEmailChange",
            new() { ["NewEmail"] = NewEmail, ["CurrentPassword"] = Password });
        var token = GetConfirmationToken();
        var pendingHtml = await Client.GetStringAsync("/account/manage");

        // Act
        await PostFormAsync(Client, "/account/manage", "/account/manage?handler=CancelEmailChange", new());
        var confirm = await Client.PostAsJsonAsync("/auth/email/change/confirm", new { Token = token });
        var profile = await Client.GetStringAsync("/account/manage");

        // Assert
        Assert.Contains("Awaiting confirmation", pendingHtml);
        Assert.Contains(NewEmail, pendingHtml);
        Assert.Equal(HttpStatusCode.BadRequest, confirm.StatusCode);
        Assert.DoesNotContain("Awaiting confirmation", profile);
        Assert.Equal(Account.Email, (await ReadAccountAsync()).Email);
    }

    [Fact]
    [Description("Discord email-change callbacks require the initiating browser session, accept the linked Discord ID, and reject callback replay.")]
    public async Task Discord_Callback_IsBoundToInitiatingBrowser()
    {
        // Arrange
        await SeedAndLoginAsync(linkDiscord: true);
        var start = await PostFormAsync(Client, "/account/manage", "/account/manage?handler=StartDiscordEmailChange",
            new() { ["NewEmail"] = NewEmail });
        Assert.Equal(HttpStatusCode.Redirect, start.StatusCode);
        var state = QueryHelpers.ParseQuery(start.Headers.Location!.Query)["state"].ToString();
        var callback = $"/auth/discord/callback?code=linked&state={Uri.EscapeDataString(state)}";
        using var otherBrowser = CreateClient();
        await LoginAsync(otherBrowser, Account.Email);

        // Act
        var missingBinding = await otherBrowser.GetAsync(callback);
        var valid = await Client.GetAsync(callback);
        var replay = await Client.GetAsync(callback);

        // Assert
        Assert.StartsWith("/account/login", missingBinding.Headers.Location!.ToString());
        Assert.StartsWith("/account/manage?status=Confirmation", valid.Headers.Location!.ToString());
        Assert.StartsWith("/account/login", replay.Headers.Location!.ToString());
        Assert.Single(Fixture.Sender.Messages, message => message.To == NewEmail);
        Assert.Equal(Account.Email, (await ReadAccountAsync()).Email);
    }

    [Fact]
    [Description("A failed password confirmation does not issue mail or reflect the submitted password in the account HTML.")]
    public async Task Request_WrongPassword_DoesNotSendMail()
    {
        // Arrange
        await SeedAndLoginAsync();
        const string wrongPassword = "WrongSecret987";

        // Act
        var response = await PostFormAsync(Client, "/account/manage", "/account/manage?handler=RequestEmailChange",
            new() { ["NewEmail"] = NewEmail, ["CurrentPassword"] = wrongPassword });

        // Assert
        var html = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.DoesNotContain(wrongPassword, html);
        Assert.DoesNotContain(Fixture.Sender.Messages, message => message.To == NewEmail);
        Assert.Equal(Account.Email, (await ReadAccountAsync()).Email);
    }

    [Fact]
    [Description("An email change revokes an issued OpenID Connect refresh token, including previously cached token and authorization entries.")]
    public async Task ChangeEmail_RejectsOldOidcRefreshToken()
    {
        // Arrange
        await SeedAndLoginAsync();
        var verifier = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
        var challenge = WebEncoders.Base64UrlEncode(SHA256.HashData(Encoding.ASCII.GetBytes(verifier)));
        var authorizeUrl = QueryHelpers.AddQueryString("/connect/authorize", new Dictionary<string, string?>
        {
            ["client_id"] = "test-public-client", ["redirect_uri"] = "https://app.akgaming.de/callback",
            ["response_type"] = "code", ["scope"] = "openid email offline_access",
            ["code_challenge"] = challenge, ["code_challenge_method"] = "S256"
        });
        var authorize = await Client.GetAsync(authorizeUrl);
        var code = QueryHelpers.ParseQuery(authorize.Headers.Location!.Query)["code"].ToString();
        Assert.False(string.IsNullOrWhiteSpace(code));
        var exchange = await Client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code", ["client_id"] = "test-public-client",
            ["redirect_uri"] = "https://app.akgaming.de/callback", ["code"] = code, ["code_verifier"] = verifier
        }));
        Assert.Equal(HttpStatusCode.OK, exchange.StatusCode);
        using var tokens = JsonDocument.Parse(await exchange.Content.ReadAsStringAsync());
        var refresh = tokens.RootElement.GetProperty("refresh_token").GetString()!;
        await PostFormAsync(Client, "/account/manage", "/account/manage?handler=RequestEmailChange",
            new() { ["NewEmail"] = NewEmail, ["CurrentPassword"] = Password });

        // Act
        var confirmation = await Client.PostAsJsonAsync("/auth/email/change/confirm", new { Token = GetConfirmationToken() });
        var response = await Client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "refresh_token", ["client_id"] = "test-public-client", ["refresh_token"] = refresh
        }));

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, confirmation.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Contains("invalid_grant", await response.Content.ReadAsStringAsync());
    }

    private HttpClient CreateClient()
    {
        return Fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions { BaseAddress = new Uri("https://localhost"), AllowAutoRedirect = false });
    }

    private async Task SeedAndLoginAsync(bool linkDiscord = false)
    {
        using var scope = Fixture.Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AuthDbContext>();
        Account.PasswordHash = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>().HashPassword(Account, Password);
        db.Users.Add(Account);
        if (linkDiscord)
            db.ExternalLogins.Add(new ExternalLogin { UserId = Account.Id, Provider = "discord", ProviderUserId = "discord-linked" });
        await db.SaveChangesAsync();
        await LoginAsync(Client, Account.Email);
    }

    private static async Task LoginAsync(HttpClient client, string email)
    {
        var response = await PostFormAsync(client, "/account/login", "/account/login", new() { ["Email"] = email, ["Password"] = Password });
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private string GetConfirmationToken()
    {
        var message = Assert.Single(Fixture.Sender.Messages, message => message.To == NewEmail);
        var link = Regex.Match(message.Text, @"https://\S+/account/email-change\?token=\S+").Value;
        return QueryHelpers.ParseQuery(new Uri(link).Query)["token"].ToString();
    }

    private async Task<User> ReadAccountAsync()
    {
        using var scope = Fixture.Factory.Services.CreateScope();
        return await scope.ServiceProvider.GetRequiredService<AuthDbContext>().Users.AsNoTracking().SingleAsync(x => x.Id == Account.Id);
    }

    private static async Task<HttpResponseMessage> PostFormAsync(HttpClient client, string formUrl, string targetUrl, Dictionary<string, string> fields)
    {
        var html = await client.GetStringAsync(formUrl);
        var match = Regex.Match(html, "name=\"__RequestVerificationToken\"[^>]*value=\"([^\"]+)\"");
        Assert.True(match.Success, "Missing antiforgery token.");
        fields["__RequestVerificationToken"] = WebUtility.HtmlDecode(match.Groups[1].Value);
        return await client.PostAsync(targetUrl, new FormUrlEncodedContent(fields));
    }

    public void Dispose()
    {
        Client.Dispose();
    }
}
