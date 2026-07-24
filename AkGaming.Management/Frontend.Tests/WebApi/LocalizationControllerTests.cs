using AkGaming.Management.Frontend.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace AkGaming.Management.Frontend.Tests.WebApi;

[TestFixture]
public sealed class LocalizationControllerTests
{
    private Mock<IUrlHelper> _urlHelper = null!;
    private DefaultHttpContext _httpContext = null!;
    private LocalizationController _controller = null!;

    [SetUp]
    public void SetUp()
    {
        _urlHelper = new Mock<IUrlHelper>();
        _httpContext = new DefaultHttpContext();
        _controller = new LocalizationController
        {
            ControllerContext = new ControllerContext { HttpContext = _httpContext },
            Url = _urlHelper.Object
        };
    }

    [TearDown]
    public void TearDown()
    {
        _controller.Dispose();
    }

    [Test]
    [Description("Selecting German stores the supported culture and redirects back to the local page.")]
    public void SetCulture_WithGermanAndLocalReturnUrl_SetsCookieAndRedirects()
    {
        // Arrange
        const string returnUrl = "/invoices/manage?draft=1";
        _urlHelper.Setup(helper => helper.IsLocalUrl(returnUrl)).Returns(true);

        // Act
        var result = _controller.SetCulture("de-DE", returnUrl);

        // Assert
        var redirect = result as LocalRedirectResult;
        Assert.That(redirect, Is.Not.Null);
        Assert.That(redirect!.Url, Is.EqualTo(returnUrl));
        Assert.That(_httpContext.Response.Headers.SetCookie.ToString(), Does.Contain(CookieRequestCultureProvider.DefaultCookieName));
        Assert.That(_httpContext.Response.Headers.SetCookie.ToString(), Does.Contain("c%3Dde-DE%7Cuic%3Dde-DE"));
    }

    [Test]
    [Description("An unsupported culture and external return URL fall back to German and the application root.")]
    public void SetCulture_WithUnsupportedCultureAndExternalReturnUrl_UsesGermanSafeFallback()
    {
        // Arrange
        const string returnUrl = "https://example.org/phishing";
        _urlHelper.Setup(helper => helper.IsLocalUrl(returnUrl)).Returns(false);

        // Act
        var result = _controller.SetCulture("fr-FR", returnUrl);

        // Assert
        var redirect = result as LocalRedirectResult;
        Assert.That(redirect, Is.Not.Null);
        Assert.That(redirect!.Url, Is.EqualTo("/"));
        Assert.That(_httpContext.Response.Headers.SetCookie.ToString(), Does.Contain("c%3Dde-DE%7Cuic%3Dde-DE"));
    }
}
