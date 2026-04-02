using AkGaming.Identity.Api.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace AkGaming.Identity.Api.Pages.Account;

public sealed class LogoutModel : PageModel
{
    public async Task<IActionResult> OnGetAsync()
    {
        await LocalSessionManager.SignOutAsync(HttpContext);
        return Redirect("/account/login");
    }
}
