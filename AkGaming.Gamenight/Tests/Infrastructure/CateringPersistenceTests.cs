using AkGaming.Gamenight.Application;
using AkGaming.Gamenight.Contracts;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;
namespace AkGaming.Gamenight.Tests.Infrastructure;
public sealed class CateringPersistenceTests : CateringFixture
{
    [Test, Description("SQLite round-trips menu trees, integer money, order timestamps, payment, and collection quantities.")]
    public async Task SqliteRoundTripsCateringState()
    {
        // Arrange
        var order = await Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Doner.Id, 2));
        var paid = await Action(order, "pay");
        await Action(paid, "collect", paid.Lines.Single().Id, 1);
        Db.ChangeTracker.Clear();
        // Act
        var menu = await Catering.MenuAsync(Event.Id, Owner, default);
        var persisted = (await Catering.OrdersAsync(Admin, false, default)).Single();
        // Assert
        Assert.That(menu.Categories, Has.Count.EqualTo(2));
        Assert.That(persisted.Lines.Single().OrderedAt, Is.EqualTo(Now));
        Assert.That(persisted.Lines.Single().Paid, Is.True);
        Assert.That(persisted.Lines.Single().CollectedQuantity, Is.EqualTo(1));
        Assert.That(persisted.Lines.Single().TotalCents, Is.EqualTo(850));
    }
    [Test, Description("SQLite optimistic concurrency rejects a competing update to an order account.")]
    public async Task ConcurrentAccountUpdateIsRejected()
    {
        // Arrange
        var order = await Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Pizza.Id, 1));
        await using var other = NewContext();
        var stale = await other.CateringAccountAsync(Visitor.Id, default);
        // Act
        await Action(order, "pay");
        stale!.EligibilityOverride = false; stale.Version = Guid.NewGuid();
        // Assert
        Assert.ThrowsAsync<GamenightException>(() => other.SaveAsync(default));
    }
    [Test, Description("Stale order versions prevent duplicate orders and stale combined settlements.")]
    public async Task StaleOrderVersionIsRejected()
    {
        // Arrange
        var stale = await Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Pizza.Id, 1));
        await Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Doner.Id, 1));
        // Act
        var error = Assert.ThrowsAsync<GamenightException>(() => Catering.SettleAsync(Visitor.Id, new(Event.Id, Visitor.Version, stale.Version, Visitor.PriceCents), Admin, default));
        // Assert
        Assert.That(error!.Status, Is.EqualTo(409));
        Assert.That((await Db.RegistrationAsync(Visitor.Id, default))!.Paid, Is.False);
    }
    [Test, Description("Event switching hides previous catering accounts and rejects forms opened for the old event.")]
    public async Task ActiveEventIsolatesCatering()
    {
        // Arrange
        var order = await Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Pizza.Id, 1));
        var next = await Service.SaveEventAsync(new EventSettings { Name = "Next", MembershipPaymentPeriodId = 1 }, Admin, default);
        var active = await Service.ActiveAsync(default);
        await Service.ActivateAsync(new(next.Id, active.SelectionVersion), Admin, default);
        // Act
        var accounts = await Catering.OrdersAsync(Admin, false, default);
        // Assert
        Assert.That(accounts, Is.Empty);
        Assert.That(await Db.Set<Domain.CateringAccount>().CountAsync(), Is.EqualTo(1));
        Assert.ThrowsAsync<GamenightException>(() => Action(order, "pay"));
    }
    [Test, Description("Nested menu categories cannot form cycles or reference nonexistent parents.")]
    public void InvalidCategoryTreeIsRejected()
    {
        // Arrange
        Menu.Categories[0].ParentId = Menu.Categories[1].Id;
        // Act
        var error = Assert.ThrowsAsync<GamenightException>(() => Catering.SaveMenuAsync(Menu, Admin, default));
        // Assert
        Assert.That(error!.Status, Is.EqualTo(400));
    }
    [Test, Description("Only code-defined icons can be stored in menu items.")]
    public void UnknownIconIsRejected()
    {
        // Arrange
        Pizza.Icon = "custom-svg";
        // Act
        var error = Assert.ThrowsAsync<GamenightException>(() => Catering.SaveMenuAsync(Menu, Admin, default));
        // Assert
        Assert.That(error!.Status, Is.EqualTo(400));
    }
    [Test, Description("A concurrent SQLite registration update rolls back the entire combined catering and admission settlement.")]
    public async Task CombinedSettlementIsAtomic()
    {
        // Arrange
        var order = await Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Pizza.Id, 1));
        await using var other = NewContext();
        var concurrent = await other.RegistrationAsync(Visitor.Id, default);
        concurrent!.Version = Guid.NewGuid();
        await other.SaveAsync(default);
        // Act
        Assert.ThrowsAsync<GamenightException>(() => Catering.SettleAsync(Visitor.Id, new(Event.Id, Visitor.Version, order.Version, Visitor.PriceCents), Admin, default));
        Db.ChangeTracker.Clear();
        var persisted = (await Catering.OrdersAsync(Admin, false, default)).Single();
        // Assert
        Assert.That(persisted.Lines.Single().Paid, Is.False);
        Assert.That((await Db.RegistrationAsync(Visitor.Id, default))!.Paid, Is.False);
    }
}
