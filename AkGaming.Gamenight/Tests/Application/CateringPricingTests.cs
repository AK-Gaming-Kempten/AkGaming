using AkGaming.Gamenight.Tests.Infrastructure;
using NUnit.Framework;
namespace AkGaming.Gamenight.Tests.Application;
public sealed class CateringPricingTests : CateringFixture
{
    [Test, Description("Exactly one meal unit receives the largest eligible discount, including per-item overrides.")]
    public async Task MaximumDiscountAppliesOnce()
    {
        // Arrange
        var pizza = new Contracts.CateringQuantity(Pizza.Id, 2);
        // Act
        var order = await Order(pizza, new(Doner.Id, 3), new(Drink.Id, 1));
        // Assert
        Assert.Multiple(() =>
        {
            Assert.That(order.Lines.Sum(l => l.DiscountCents), Is.EqualTo(350));
            Assert.That(order.Lines.Single(l => l.ItemId == Doner.Id).DiscountApplied, Is.True);
            Assert.That(order.OutstandingCents, Is.EqualTo(3350));
        });
    }
    [Test, Description("Adding a better discounted meal reallocates the discount across unpaid orders.")]
    public async Task LaterUnpaidMealCanReceiveHigherDiscount()
    {
        // Arrange
        await Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Pizza.Id, 1));
        // Act
        var order = await Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Doner.Id, 1));
        // Assert
        Assert.That(order.Lines.Single(l => l.ItemId == Pizza.Id).DiscountCents, Is.Zero);
        Assert.That(order.Lines.Single(l => l.ItemId == Doner.Id).DiscountCents, Is.EqualTo(350));
    }
    [Test, Description("A paid discounted meal consumes the single event discount and keeps its settled price.")]
    public async Task PaidDiscountCannotBeUsedAgain()
    {
        // Arrange
        var order = await Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Pizza.Id, 1));
        await Action(order, "pay");
        // Act
        var updated = await Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Doner.Id, 1));
        // Assert
        Assert.That(updated.Lines.Sum(l => l.DiscountCents), Is.EqualTo(200));
        Assert.That(updated.OutstandingCents, Is.EqualTo(600));
    }
    [Test, Description("Item discounts are capped at one unit's price and never make an order negative.")]
    public async Task DiscountIsCappedAtUnitPrice()
    {
        // Arrange
        Doner.DiscountOverrideCents = 1000;
        Menu = await Catering.SaveMenuAsync(Menu, Admin, default);
        // Act
        var order = await Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Doner.Id, 2));
        // Assert
        Assert.That(order.OutstandingCents, Is.EqualTo(600));
        Assert.That(order.Lines.Single().DiscountCents, Is.EqualTo(600));
    }
    [Test, Description("Noneligible items and explicit zero overrides never receive the default meal discount.")]
    public async Task ZeroOverrideAndExcludedItemsAreRespected()
    {
        // Arrange
        Pizza.DiscountOverrideCents = 0;
        Menu = await Catering.SaveMenuAsync(Menu, Admin, default);
        // Act
        var order = await Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Pizza.Id, 1), new(Drink.Id, 1));
        // Assert
        Assert.That(order.Lines.Sum(l => l.DiscountCents), Is.Zero);
    }
    [Test, Description("Late registrations receive no discount until frontdesk explicitly overrides eligibility with a reason.")]
    public async Task FrontdeskCanGrantEligibility()
    {
        // Arrange
        Event.EarlyBirdDeadline = Now.AddDays(-1);
        await Service.SaveEventAsync(Event, Admin, default);
        var order = await Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Pizza.Id, 1));
        // Act
        var changed = await Action(order, "eligibility", eligibility: true, reason: "Anmeldung war rechtzeitig eingegangen");
        // Assert
        Assert.That(order.OutstandingCents, Is.EqualTo(800));
        Assert.That(changed.OutstandingCents, Is.EqualTo(600));
        Assert.That(Db.Audit.Any(a => a.Action.Contains("eligibility=True")), Is.True);
    }
    [Test, Description("Removing and restoring an eligibility override recalculates open meal prices.")]
    public async Task EligibilityOverrideCanBeRemoved()
    {
        // Arrange
        var order = await Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Pizza.Id, 1));
        var disabled = await Action(order, "eligibility", eligibility: false, reason: "Korrektur");
        // Act
        var restored = await Action(disabled, "eligibility", reason: "Automatische Prüfung");
        // Assert
        Assert.That(disabled.OutstandingCents, Is.EqualTo(800));
        Assert.That(restored.OutstandingCents, Is.EqualTo(600));
    }
    [Test, Description("Menu changes preserve existing order names, category paths, prices, and discount snapshots.")]
    public async Task MenuEditsDoNotRepriceOrders()
    {
        // Arrange
        await Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Doner.Id, 1));
        Doner.Name = "Neuer Name"; Doner.PriceCents = 1000; Doner.DiscountOverrideCents = 100;
        Menu = await Catering.SaveMenuAsync(Menu, Admin, default);
        Db.ChangeTracker.Clear();
        // Act
        var order = (await Catering.OrdersAsync(Owner, true, default)).Single();
        // Assert
        Assert.That(order.Lines.Single().ItemName, Is.EqualTo("Döner"));
        Assert.That(order.Lines.Single().CategoryName, Is.EqualTo("Essen / Gerichte"));
        Assert.That(order.OutstandingCents, Is.EqualTo(250));
    }
    [Test, Description("Cancelling the discounted unpaid meal reallocates its discount to the remaining eligible meal.")]
    public async Task CancellationReallocatesDiscount()
    {
        // Arrange
        var order = await Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Pizza.Id, 1), new(Doner.Id, 1));
        // Act
        var changed = await Action(order, "cancel", order.Lines.Single(l => l.ItemId == Doner.Id).Id);
        // Assert
        Assert.That(changed.Lines.Single().DiscountCents, Is.EqualTo(200));
    }
}
