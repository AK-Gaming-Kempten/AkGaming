using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace AkGaming.Identity.Api.OpenIddict;

internal sealed class OpenCloudTokenCompatibilityMiddleware
{
    private const string TokenEndpointPath = "/connect/token";
    private const string OpenCloudClientIdPrefix = "OpenCloud";
    private const string AuthorizationCodeGrantType = "authorization_code";
    private const string ScopeParameter = "scope";

    private readonly RequestDelegate _next;
    private readonly ILogger<OpenCloudTokenCompatibilityMiddleware> _logger;

    public OpenCloudTokenCompatibilityMiddleware(
        RequestDelegate next,
        ILogger<OpenCloudTokenCompatibilityMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (!ShouldInspect(context.Request))
        {
            await _next(context);
            return;
        }

        context.Request.EnableBuffering();

        using var reader = new StreamReader(
            context.Request.Body,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: false,
            bufferSize: 1024,
            leaveOpen: true);
        var body = await reader.ReadToEndAsync(context.RequestAborted);
        context.Request.Body.Position = 0;

        if (string.IsNullOrWhiteSpace(body))
        {
            await _next(context);
            return;
        }

        var form = QueryHelpers.ParseQuery(body);
        if (!ShouldRewrite(form, out var clientId))
        {
            await _next(context);
            return;
        }

        var fields = form
            .Where(field => !string.Equals(field.Key, ScopeParameter, StringComparison.Ordinal))
            .SelectMany(field => field.Value.Count == 0
                ? [new KeyValuePair<string, string>(field.Key, string.Empty)]
                : field.Value.Select(value => new KeyValuePair<string, string>(field.Key, value ?? string.Empty)));

        using var content = new FormUrlEncodedContent(fields);
        var rewrittenBody = await content.ReadAsByteArrayAsync(context.RequestAborted);

        context.Request.Body = new MemoryStream(rewrittenBody);
        context.Request.ContentLength = rewrittenBody.Length;

        _logger.LogInformation(
            "Removed token request scope parameter for OpenCloud client {ClientId} authorization-code exchange.",
            clientId);

        await _next(context);
    }

    private static bool ShouldInspect(HttpRequest request)
    {
        return HttpMethods.IsPost(request.Method)
               && request.Path.Equals(TokenEndpointPath, StringComparison.Ordinal)
               && request.HasFormContentType;
    }

    private static bool ShouldRewrite(
        IDictionary<string, Microsoft.Extensions.Primitives.StringValues> form,
        out string clientId)
    {
        clientId = string.Empty;

        if (!form.TryGetValue("grant_type", out var grantTypes)
            || !string.Equals(grantTypes.ToString(), AuthorizationCodeGrantType, StringComparison.Ordinal))
        {
            return false;
        }

        if (!form.TryGetValue("client_id", out var clientIds))
        {
            return false;
        }

        clientId = clientIds.ToString();

        return clientId.StartsWith(OpenCloudClientIdPrefix, StringComparison.OrdinalIgnoreCase)
               && form.ContainsKey(ScopeParameter);
    }
}

internal static class OpenCloudTokenCompatibilityMiddlewareExtensions
{
    public static IApplicationBuilder UseOpenCloudTokenCompatibility(this IApplicationBuilder app)
    {
        return app.UseMiddleware<OpenCloudTokenCompatibilityMiddleware>();
    }
}
