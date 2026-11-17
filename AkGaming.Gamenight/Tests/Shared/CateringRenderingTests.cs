using System.Net;
using AkGaming.Gamenight.Contracts;
using AkGaming.Gamenight.Shared;
using AkGaming.Gamenight.Shared.Components;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;
namespace AkGaming.Gamenight.Tests.Shared;
public sealed class CateringRenderingTests
{
    private ServiceProvider Services { get; set; } = default!;
    private HtmlRenderer Renderer { get; set; } = default!;
    private CateringLine Line { get; set; } = default!;
    private CateringView Account { get; set; } = default!;
    [SetUp]
    public void Setup()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(new GamenightClient(new HttpClient(), Mock.Of<IUserSession>()));
        Services = services.BuildServiceProvider();
        Renderer = new HtmlRenderer(Services, Services.GetRequiredService<ILoggerFactory>());
        Line = new CateringLine { ItemId = Guid.NewGuid(), ItemName = "Döner", CategoryName = "Essen", Icon = "doner", UnitPriceCents = 700, Quantity = 3, OrderedAt = DateTime.UtcNow };
        Account = new CateringView(Guid.NewGuid(), Guid.NewGuid(), "Alex Visitor", "visitor@example.com", false, true, null, [Line]);
    }
    private async Task<string> Render<T>(Dictionary<string, object?> parameters) where T : IComponent =>
        await Renderer.Dispatcher.InvokeAsync(async () => WebUtility.HtmlDecode((await Renderer.RenderComponentAsync<T>(ParameterView.FromDictionary(parameters))).ToHtmlString()));
    [Test, Description("The supplier overview explicitly labels unpaid quantities while retaining paid quantities in the total.")]
    public async Task SupplierOverviewHighlightsUnpaidMeals()
    {
        // Arrange
        Account.Lines.Add(new CateringLine { ItemId = Line.ItemId, ItemName = Line.ItemName, CategoryName = Line.CategoryName, Quantity = 2, Paid = true });
        // Act
        var html = await Render<CateringSupplierOverview>(new() { ["Accounts"] = new List<CateringView> { Account } });
        // Assert
        Assert.That(html, Does.Contain("3 UNBEZAHLT"));
        Assert.That(html, Does.Contain("<td>5</td><td>2</td>"));
        Assert.That(html, Does.Contain("Achtung: unbezahlte Bestellungen enthalten."));
    }
    [Test, Description("The collection button is visibly disabled for unpaid orders and meal SVG artwork is rendered.")]
    public async Task UnpaidCollectionIsDisabled()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["Line"] = Line, ["Staff"] = true };
        // Act
        var html = await Render<CateringOrderLine>(parameters);
        // Assert
        Assert.That(html, Does.Contain("disabled"));
        Assert.That(html, Does.Contain("Abholung erst nach Bezahlung"));
        Assert.That(html, Does.Contain("UNBEZAHLT"));
        Assert.That(html, Does.Contain("fill=\"#d69b56\""));
    }
    [Test, Description("Frontdesk displays admission plus outstanding catering and exposes one combined payment action.")]
    public async Task FrontdeskShowsCombinedBalance()
    {
        // Arrange
        var registration = new RegistrationView(Account.RegistrationId, Guid.NewGuid(), new(), Guid.NewGuid(), false, false, false, false, 500, "Frühbucher", DateTime.UtcNow, false, false);
        // Act
        var html = await Render<FrontdeskVisitorDetails>(new() { ["Catering"] = Account, ["Menu"] = new CateringMenu(), ["Registration"] = registration, ["Granted"] = new HashSet<string> { Permissions.Frontdesk } });
        // Assert
        Assert.That(html, Does.Contain("Gesamt zu zahlen"));
        Assert.That(html, Does.Contain("26,00"));
        Assert.That(html, Does.Contain("Gesamtzahlung erfassen"));
    }
    [Test, Description("Read-only order viewers do not see order creation or cancellation controls.")]
    public async Task ReadOnlyViewerHasNoOrderActions()
    {
        // Arrange
        var parameters = new Dictionary<string, object?> { ["Account"] = Account, ["Menu"] = new CateringMenu { OrdersOpen = true }, ["CanOrder"] = false };
        // Act
        var html = await Render<CateringAccountPanel>(parameters);
        // Assert
        Assert.That(html, Does.Not.Contain("Essen bestellen"));
        Assert.That(html, Does.Not.Contain("Stornieren"));
    }
    [TearDown]
    public async Task Cleanup()
    {
        await Renderer.DisposeAsync();
        await Services.DisposeAsync();
    }
}
