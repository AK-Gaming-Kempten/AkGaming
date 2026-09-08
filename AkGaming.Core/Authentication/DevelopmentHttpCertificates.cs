using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace AkGaming.Core.Authentication;

/// <summary>Explicit development-only support for untrusted localhost HTTPS roots.</summary>
public static class DevelopmentHttpCertificates
{
    public static HttpClientHandler CreateHandler(IHostEnvironment environment, IConfiguration configuration)
    {
        var handler = new HttpClientHandler();
        Configure(handler, environment, configuration);
        return handler;
    }

    public static void Configure(HttpClientHandler handler, IHostEnvironment environment, IConfiguration configuration)
    {
        if (!environment.IsDevelopment() || !configuration.GetValue<bool>("Dev:AllowUntrustedLocalCertificates"))
            return;

        handler.ServerCertificateCustomValidationCallback = (request, _, chain, errors) =>
            AcceptCertificate(request.RequestUri, errors, chain?.ChainStatus ?? []);
    }

    public static bool AcceptCertificate(Uri? uri, SslPolicyErrors errors, X509ChainStatus[] chainStatus)
    {
        if (errors == SslPolicyErrors.None)
            return true;

        if (uri?.Scheme != Uri.UriSchemeHttps || errors != SslPolicyErrors.RemoteCertificateChainErrors)
            return false;

        var local = string.Equals(uri.Host, "localhost", StringComparison.OrdinalIgnoreCase)
                    || uri.Host == "127.0.0.1" || uri.Host == "[::1]";
        return local && chainStatus.Length > 0
                     && chainStatus.All(status => status.Status == X509ChainStatusFlags.UntrustedRoot);
    }
}
