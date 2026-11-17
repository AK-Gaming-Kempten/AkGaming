using AkGaming.Gamenight.Application;
using AkGaming.Gamenight.Contracts;
using Moq;
using NUnit.Framework;

namespace AkGaming.Gamenight.Tests.Infrastructure;
public abstract class CateringFixture : RegistrationFixture
{
    protected CateringService Catering { get; private set; } = default!;
    protected CateringMenu Menu { get; set; } = default!;
    protected RegistrationView Visitor { get; set; } = default!;
    protected CateringItem Pizza => Menu.Items[0];
    protected CateringItem Doner => Menu.Items[1];
    protected CateringItem Drink => Menu.Items[2];
    [SetUp]
    public async Task SetupCatering()
    {
        var clock = new Mock<TimeProvider>();
        clock.Setup(c => c.GetUtcNow()).Returns(new DateTimeOffset(Now));
        Catering = new CateringService(Db, Service, clock.Object);
        Visitor = await Register(Owner);
        var category = new CateringCategory { Name = "Essen" };
        var child = new CateringCategory { Name = "Gerichte", ParentId = category.Id };
        Menu = await Catering.SaveMenuAsync(new CateringMenu
        {
            EventId = Event.Id, OrdersOpen = true, DefaultDiscountCents = 200,
            Categories = [category, child],
            Items = [new() { Name = "Pizza", CategoryId = category.Id, PriceCents = 800 },
                new() { Name = "Döner", CategoryId = child.Id, PriceCents = 600, Icon = "doner", DiscountOverrideCents = 350 },
                new() { Name = "Getränk", CategoryId = category.Id, PriceCents = 300, DiscountEligible = false }]
        }, Admin, default);
    }
    protected async Task<CateringView> Order(params CateringQuantity[] quantities)
    {
        var current = (await Catering.OrdersAsync(Owner, true, default)).Single();
        return await Catering.PlaceAsync(Visitor.Id, new(Event.Id, Menu.Version, current.Version, quantities.ToList()), Owner, default);
    }
    protected Task<CateringView> Action(CateringView view, string action, Guid? lineId = null, int? quantity = null, bool? eligibility = null, string? reason = null) =>
        Catering.ActionAsync(Visitor.Id, new(Event.Id, view.Version, action, lineId, quantity, eligibility, reason), Admin, default);
}
