using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using AkGaming.Core.Authentication;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Moq;
using NUnit.Framework;

namespace AkGaming.Gamenight.Tests.Infrastructure;

public sealed class DevelopmentHttpCertificatesTests
{
    private Mock<IHostEnvironment> Environment { get; set; } = default!;
    private IConfiguration Configuration { get; set; } = default!;

    [SetUp]
    public void Setup()
    {
        Environment = new Mock<IHostEnvironment>();
        Environment.SetupGet(e => e.EnvironmentName).Returns(Environments.Development);
        Configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Dev:AllowUntrustedLocalCertificates"] = "true" }).Build();
    }

    [TestCase("https://localhost:7288")]
    [TestCase("https://127.0.0.1:7288")]
    [TestCase("https://[::1]:7288")]
    [Description("An explicitly enabled development handler accepts only untrusted localhost roots.")]
    public void LocalUntrustedRootIsAccepted(string address)
    {
        // Arrange
        var statuses = new[] { new X509ChainStatus { Status = X509ChainStatusFlags.UntrustedRoot } };
        using var handler = DevelopmentHttpCertificates.CreateHandler(Environment.Object, Configuration);
        // Act
        var accepted = DevelopmentHttpCertificates.AcceptCertificate(new Uri(address), SslPolicyErrors.RemoteCertificateChainErrors, statuses);
        // Assert
        Assert.That(handler.ServerCertificateCustomValidationCallback, Is.Not.Null);
        Assert.That(accepted, Is.True);
    }

    [TestCase("https://identity.akgaming.de", SslPolicyErrors.RemoteCertificateChainErrors, X509ChainStatusFlags.UntrustedRoot)]
    [TestCase("https://localhost.example.com", SslPolicyErrors.RemoteCertificateChainErrors, X509ChainStatusFlags.UntrustedRoot)]
    [TestCase("https://localhost", SslPolicyErrors.RemoteCertificateNameMismatch | SslPolicyErrors.RemoteCertificateChainErrors, X509ChainStatusFlags.UntrustedRoot)]
    [TestCase("https://localhost", SslPolicyErrors.RemoteCertificateChainErrors, X509ChainStatusFlags.NotTimeValid | X509ChainStatusFlags.UntrustedRoot)]
    [TestCase("https://localhost", SslPolicyErrors.RemoteCertificateChainErrors, X509ChainStatusFlags.PartialChain)]
    [TestCase("https://localhost", SslPolicyErrors.RemoteCertificateNotAvailable, X509ChainStatusFlags.UntrustedRoot)]
    [Description("Remote hosts, mismatched names, expired certificates, and other TLS failures stay rejected.")]
    public void OtherTlsFailuresAreRejected(string address, SslPolicyErrors errors, X509ChainStatusFlags status)
    {
        // Arrange
        var statuses = new[] { new X509ChainStatus { Status = status } };
        // Act
        var accepted = DevelopmentHttpCertificates.AcceptCertificate(new Uri(address), errors, statuses);
        // Assert
        Assert.That(accepted, Is.False);
    }

    [TestCase("Production", true)]
    [TestCase("Staging", true)]
    [TestCase("Development", false)]
    [Description("Production, staging, and development without opt-in keep platform certificate validation.")]
    public void ValidationIsNotOverriddenWithoutBothDevelopmentAndOptIn(string environment, bool enabled)
    {
        // Arrange
        Environment.SetupGet(e => e.EnvironmentName).Returns(environment);
        Configuration = new ConfigurationBuilder().AddInMemoryCollection(
            new Dictionary<string, string?> { ["Dev:AllowUntrustedLocalCertificates"] = enabled.ToString() }).Build();
        // Act
        using var handler = DevelopmentHttpCertificates.CreateHandler(Environment.Object, Configuration);
        // Assert
        Assert.That(handler.ServerCertificateCustomValidationCallback, Is.Null);
    }

    [Test, Description("Normally valid remote certificates continue to pass when the development option is enabled.")]
    public void ValidRemoteCertificateIsAccepted()
    {
        // Arrange
        var uri = new Uri("https://identity.akgaming.de");
        // Act
        var accepted = DevelopmentHttpCertificates.AcceptCertificate(uri, SslPolicyErrors.None, []);
        // Assert
        Assert.That(accepted, Is.True);
    }
}
