using AkGaming.Identity.Api.Endpoints;
using Microsoft.Extensions.Configuration;

namespace AkGaming.Identity.Api.IntegrationTests;

public sealed class EndpointUtilitiesTests
{
    [Fact]
    public void IsAllowedRedirectUri_ExactMatch_Allows()
    {
        var config = BuildConfig("https://management.akgaming.de/authentication/callback");

        var allowed = EndpointUtilities.IsAllowedRedirectUri(
            "https://management.akgaming.de/authentication/callback",
            config,
            out var reason,
            out _);

        Assert.True(allowed);
        Assert.Equal("matched_allowlist", reason);
    }

    [Fact]
    public void IsAllowedRedirectUri_ExactMismatch_Denies()
    {
        var config = BuildConfig("https://management.akgaming.de/authentication/callback");

        var allowed = EndpointUtilities.IsAllowedRedirectUri(
            "https://management.akgaming.de/other",
            config,
            out var reason,
            out _);

        Assert.False(allowed);
        Assert.Equal("not_in_allowlist", reason);
    }

    [Fact]
    public void IsAllowedRedirectUri_WildcardHost_AllowsSubdomain()
    {
        var config = BuildConfig("https://*.akgaming.de");

        var allowed = EndpointUtilities.IsAllowedRedirectUri(
            "https://management.akgaming.de/authentication/callback",
            config,
            out var reason,
            out _);

        Assert.True(allowed);
        Assert.Equal("matched_allowlist", reason);
    }

    [Fact]
    public void IsAllowedRedirectUri_WildcardHost_DeniesApexDomain()
    {
        var config = BuildConfig("https://*.akgaming.de");

        var allowed = EndpointUtilities.IsAllowedRedirectUri(
            "https://akgaming.de/authentication/callback",
            config,
            out var reason,
            out _);

        Assert.False(allowed);
        Assert.Equal("not_in_allowlist", reason);
    }

    [Fact]
    public void IsAllowedRedirectUri_WildcardWithPath_RequiresExactPath()
    {
        var config = BuildConfig("https://*.akgaming.de/authentication/callback");

        var allowedExact = EndpointUtilities.IsAllowedRedirectUri(
            "https://management.akgaming.de/authentication/callback",
            config,
            out var reasonExact,
            out _);

        var allowedWrongPath = EndpointUtilities.IsAllowedRedirectUri(
            "https://management.akgaming.de/authentication/other",
            config,
            out var reasonWrongPath,
            out _);

        Assert.True(allowedExact);
        Assert.Equal("matched_allowlist", reasonExact);
        Assert.False(allowedWrongPath);
        Assert.Equal("not_in_allowlist", reasonWrongPath);
    }

    [Fact]
    public void IsAllowedRedirectUri_WildcardPort_RequiresMatchingPort()
    {
        var config = BuildConfig("https://*.akgaming.de:8443/authentication/callback");

        var allowed = EndpointUtilities.IsAllowedRedirectUri(
            "https://management.akgaming.de:8443/authentication/callback",
            config,
            out var reasonAllowed,
            out _);

        var denied = EndpointUtilities.IsAllowedRedirectUri(
            "https://management.akgaming.de/authentication/callback",
            config,
            out var reasonDenied,
            out _);

        Assert.True(allowed);
        Assert.Equal("matched_allowlist", reasonAllowed);
        Assert.False(denied);
        Assert.Equal("not_in_allowlist", reasonDenied);
    }

    [Fact]
    public void IsAllowedRedirectUri_DeniesInvalidUri()
    {
        var config = BuildConfig("https://*.akgaming.de");

        var allowed = EndpointUtilities.IsAllowedRedirectUri(
            "not-a-uri",
            config,
            out var reason,
            out var evaluations);

        Assert.False(allowed);
        Assert.Equal("invalid_uri", reason);
        Assert.Empty(evaluations);
    }

    [Fact]
    public void IsAllowedRedirectUri_DeniesInvalidScheme()
    {
        var config = BuildConfig("https://*.akgaming.de");

        var allowed = EndpointUtilities.IsAllowedRedirectUri(
            "ftp://management.akgaming.de/authentication/callback",
            config,
            out var reason,
            out var evaluations);

        Assert.False(allowed);
        Assert.Equal("invalid_scheme", reason);
        Assert.Empty(evaluations);
    }

    private static IConfiguration BuildConfig(params string[] entries)
    {
        var dict = entries
            .Select((value, index) => new KeyValuePair<string, string?>($"Bridge:AllowedRedirectUris:{index}", value))
            .ToDictionary(x => x.Key, x => x.Value);

        return new ConfigurationBuilder()
            .AddInMemoryCollection(dict)
            .Build();
    }
}
