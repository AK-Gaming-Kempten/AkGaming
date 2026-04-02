using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore;
using OpenIddict.Server.AspNetCore;

namespace AkGaming.Identity.Api.Pages;

public sealed class ErrorModel : PageModel
{
    public string Message { get; private set; } = "The request could not be completed.";
    public string? Error { get; private set; }

    public void OnGet()
    {
        var response = HttpContext.GetOpenIddictServerResponse();
        if (response is not null)
        {
            Error = response.ErrorDescription ?? response.Error;
            return;
        }

        var feature = HttpContext.Features.Get<IStatusCodeReExecuteFeature>();
        if (feature is not null)
        {
            Message = $"The request for '{feature.OriginalPath}' failed.";
        }
    }
}
