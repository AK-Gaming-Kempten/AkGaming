using Microsoft.AspNetCore.Authentication;
using System.Net.Http.Headers;

namespace Frontend.Blazor.Handlers;

public class ApiAuthorizationHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _contextAccessor;

    public ApiAuthorizationHandler(IHttpContextAccessor contextAccessor)
    {
        _contextAccessor = contextAccessor;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken)
    {
        var token = await _contextAccessor.HttpContext!.GetTokenAsync("access_token");

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}