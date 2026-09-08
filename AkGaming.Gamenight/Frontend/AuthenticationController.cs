using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
namespace AkGaming.Gamenight.Frontend;
[AllowAnonymous]
public sealed class AuthenticationController : Controller
{
    [HttpGet("/authentication/login")]
    public IActionResult Login()
    {
        var properties = new AuthenticationProperties { RedirectUri = "/mine" };
        return Challenge(properties, OpenIdConnectDefaults.AuthenticationScheme);
    }
    [HttpGet("/authentication/logout")]
    public IActionResult LogoutConfirmation()
    {
        return Content("<!doctype html><html lang='de'><meta charset='utf-8'><title>Abmelden</title><p>Zum Abmelden bitte den Button auf der Bestätigungsseite verwenden.</p><a href='/logout'>Weiter</a></html>", "text/html");
    }
    [HttpPost("/authentication/logout"), ValidateAntiForgeryToken]
    public IActionResult Logout()
    {
        var properties = new AuthenticationProperties { RedirectUri = "/" };
        return SignOut(properties, CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
    }
    [HttpGet("/error")]
    public IActionResult Error()
    {
        return Problem("Die Seite konnte nicht geladen werden. Bitte erneut versuchen.");
    }
}

