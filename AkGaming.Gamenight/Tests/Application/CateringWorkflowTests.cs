using AkGaming.Gamenight.Application;
using AkGaming.Gamenight.Contracts;
using AkGaming.Gamenight.Tests.Infrastructure;
using NUnit.Framework;
namespace AkGaming.Gamenight.Tests.Application;
public sealed class CateringWorkflowTests : CateringFixture
{
    [Test, Description("Locked ordering rejects attendees but allows a warned frontdesk operator to explicitly acknowledge the lock.")]
    public async Task LockedOrderingRequiresStaffAcknowledgement()
    {
        // Arrange
        Menu.OrdersOpen = false;
        Menu = await Catering.SaveMenuAsync(Menu, Admin, default);
        var request = new PlaceCateringOrder(Event.Id, Menu.Version, Guid.Empty, [new(Pizza.Id, 1)]);
        // Act
        var visitorError = Assert.ThrowsAsync<GamenightException>(() => Catering.PlaceAsync(Visitor.Id, request, Owner, default));
        var staffError = Assert.ThrowsAsync<GamenightException>(() => Catering.PlaceAsync(Visitor.Id, request, Admin, default));
        var order = await Catering.PlaceAsync(Visitor.Id, request with { AcknowledgeLocked = true }, Admin, default);
        // Assert
        Assert.That(visitorError!.Status, Is.EqualTo(409));
        Assert.That(staffError!.Status, Is.EqualTo(409));
        Assert.That(order.Lines, Has.Count.EqualTo(1));
        Assert.That(Db.Audit.Any(a => a.Action == "catering.ordered.lock-override"), Is.True);
    }
    [Test, Description("Collection is rejected until the order is paid, and paid orders support partial collection.")]
    public async Task PaymentIsRequiredForCollection()
    {
        // Arrange
        var order = await Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Pizza.Id, 3));
        // Act
        var error = Assert.ThrowsAsync<GamenightException>(() => Action(order, "collect", order.Lines.Single().Id, 1));
        var paid = await Action(order, "pay");
        var partial = await Action(paid, "collect", paid.Lines.Single().Id, 1);
        // Assert
        Assert.That(error!.Status, Is.EqualTo(409));
        Assert.That(partial.Lines.Single().CollectedQuantity, Is.EqualTo(1));
        Assert.ThrowsAsync<GamenightException>(() => Action(partial, "collect", partial.Lines.Single().Id, 3));
    }
    [Test, Description("An attendee cannot pay, collect, or override their own eligibility.")]
    public async Task StaffActionsRequireStaffPermission()
    {
        // Arrange
        var order = await Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Pizza.Id, 1));
        // Act
        var errors = new[] { "pay", "collect", "eligibility" }.Select(action =>
            Assert.ThrowsAsync<GamenightException>(() => Catering.ActionAsync(Visitor.Id, new(Event.Id, order.Version, action, order.Lines.Single().Id, 1, true, "test"), Owner, default))).ToList();
        // Assert
        Assert.That(errors.All(e => e!.Status == 403), Is.True);
    }
    [Test, Description("Another signed-in user cannot place orders against somebody else's registration.")]
    public void OwnershipIsEnforced()
    {
        // Arrange
        var stranger = new Actor("stranger", "stranger@example.com", new HashSet<string>());
        var request = new PlaceCateringOrder(Event.Id, Menu.Version, Guid.Empty, [new(Pizza.Id, 1)]);
        // Act
        var error = Assert.ThrowsAsync<GamenightException>(() => Catering.PlaceAsync(Visitor.Id, request, stranger, default));
        // Assert
        Assert.That(error!.Status, Is.EqualTo(403));
    }
    [Test, Description("A logged-in user without a registration sees no order account and cannot order against an invented registration.")]
    public async Task RegistrationIsRequired()
    {
        // Arrange
        var stranger = new Actor("stranger", "stranger@example.com", new HashSet<string>());
        // Act
        var accounts = await Catering.OrdersAsync(stranger, true, default);
        // Assert
        Assert.That(accounts, Is.Empty);
        Assert.ThrowsAsync<GamenightException>(() => Catering.PlaceAsync(Guid.NewGuid(), new(Event.Id, Menu.Version, Guid.Empty, [new(Pizza.Id, 1)]), stranger, default));
    }
    [Test, Description("Frontdesk can order for guest registrations without assigning account ownership.")]
    public async Task FrontdeskCanOrderForGuest()
    {
        // Arrange
        await Service.SubmitAsync(new(Event.Id, Form("guest@example.com")), Guest, default);
        var guest = (await Service.DeskAsync(Admin, default)).Single(r => r.Form.Email == "guest@example.com");
        // Act
        var order = await Catering.PlaceAsync(guest.Id, new(Event.Id, Menu.Version, Guid.Empty, [new(Pizza.Id, 1)]), Admin, default);
        // Assert
        Assert.That(order.Person, Is.EqualTo("Alex Visitor"));
        Assert.That((await Db.RegistrationAsync(guest.Id, default))!.OwnerId, Is.Null);
    }
    [Test, Description("A verified email claims an existing guest registration before self-service catering.")]
    public async Task GuestRegistrationCanBeClaimed()
    {
        // Arrange
        await Service.SubmitAsync(new(Event.Id, Form("claim@example.com")), Guest, default);
        var claimant = new Actor("claimant", "claim@example.com", new HashSet<string>());
        // Act
        var accounts = await Catering.OrdersAsync(claimant, true, default);
        // Assert
        Assert.That(accounts, Has.Count.EqualTo(1));
        Assert.That(accounts.Single().Email, Is.EqualTo("claim@example.com"));
    }
    [Test, Description("Combined settlement marks admission and all outstanding catering as paid while preserving entry-only settled prices.")]
    public async Task CombinedSettlementKeepsSeparateBalances()
    {
        // Arrange
        var order = await Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Pizza.Id, 1));
        // Act
        await Catering.SettleAsync(Visitor.Id, new(Event.Id, Visitor.Version, order.Version, Visitor.PriceCents), Admin, default);
        Db.ChangeTracker.Clear();
        var registration = (await Service.DeskAsync(Admin, default)).Single();
        var settled = (await Catering.OrdersAsync(Admin, false, default)).Single();
        // Assert
        Assert.That(registration.Paid, Is.True);
        Assert.That(registration.PriceCents, Is.EqualTo(Visitor.PriceCents));
        Assert.That(settled.OutstandingCents, Is.Zero);
        Assert.That(settled.Lines.All(l => l.Paid), Is.True);
    }
    [Test, Description("New catering after entry payment creates only a new catering balance.")]
    public async Task AdditionalOrderDoesNotReopenAdmission()
    {
        // Arrange
        await Service.ActionAsync(Visitor.Id, new(Visitor.Version, "pay"), Admin, default);
        // Act
        var order = await Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Doner.Id, 1));
        // Assert
        Assert.That(order.OutstandingCents, Is.EqualTo(250));
        Assert.That((await Service.DeskAsync(Admin, default)).Single().Paid, Is.True);
    }
    [Test, Description("Paid positions cannot be cancelled and collection corrections require a reason.")]
    public async Task SettledOrdersRequireControlledCorrections()
    {
        // Arrange
        var order = await Action(await Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Pizza.Id, 1)), "pay");
        // Act
        var collected = await Action(order, "collect", order.Lines.Single().Id, 1);
        // Assert
        Assert.ThrowsAsync<GamenightException>(() => Action(collected, "cancel", collected.Lines.Single().Id));
        Assert.ThrowsAsync<GamenightException>(() => Action(collected, "uncollect", collected.Lines.Single().Id));
    }
    [Test, Description("Meal unavailability and stale menu versions are enforced by the server.")]
    public async Task UnavailableAndStaleMenuAreRejected()
    {
        // Arrange
        var staleVersion = Menu.Version;
        Pizza.Available = false;
        Menu = await Catering.SaveMenuAsync(Menu, Admin, default);
        // Act
        var unavailable = Assert.ThrowsAsync<GamenightException>(() => Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Pizza.Id, 1)));
        var stale = Assert.ThrowsAsync<GamenightException>(() => Catering.PlaceAsync(Visitor.Id, new(Event.Id, staleVersion, Guid.Empty, [new(Doner.Id, 1)]), Owner, default));
        // Assert
        Assert.That(unavailable!.Status, Is.EqualTo(409));
        Assert.That(stale!.Status, Is.EqualTo(409));
    }
    [Test, Description("Cancelling a registration keeps supplier-visible order records and prevents new orders.")]
    public async Task CancelledRegistrationRetainsExistingOrders()
    {
        // Arrange
        await Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Pizza.Id, 1));
        await Service.ActionAsync(Visitor.Id, new(Visitor.Version, "cancel"), Admin, default);
        // Act
        var account = (await Catering.OrdersAsync(Admin, false, default)).Single();
        // Assert
        Assert.That(account.Cancelled, Is.True);
        Assert.That(account.Lines, Has.Count.EqualTo(1));
        Assert.ThrowsAsync<GamenightException>(() => Order(new AkGaming.Gamenight.Contracts.CateringQuantity(Pizza.Id, 1)));
    }
    [Test, Description("A changed admission price is rejected before frontdesk settles a previously displayed amount.")]
    public async Task ChangedAdmissionPriceRequiresReview()
    {
        // Arrange
        var order = await Order(new CateringQuantity(Pizza.Id, 1));
        Event.EarlyPriceCents += 100;
        await Service.SaveEventAsync(Event, Admin, default);
        // Act
        var error = Assert.ThrowsAsync<GamenightException>(() => Catering.SettleAsync(Visitor.Id,
            new(Event.Id, Visitor.Version, order.Version, Visitor.PriceCents), Admin, default));
        // Assert
        Assert.That(error!.Status, Is.EqualTo(409));
        Assert.That((await Db.RegistrationAsync(Visitor.Id, default))!.Paid, Is.False);
    }
    [Test, Description("Frontdesk eligibility changes require a reason and preserve previously paid prices.")]
    public async Task EligibilityChangesPreservePaidPrices()
    {
        // Arrange
        var paid = await Action(await Order(new CateringQuantity(Pizza.Id, 1)), "pay");
        // Act
        var changed = await Action(paid, "eligibility", eligibility: false, reason: "Korrektur für künftige Bestellungen");
        // Assert
        Assert.That(changed.DiscountEligible, Is.False);
        Assert.That(changed.Lines.Single().TotalCents, Is.EqualTo(600));
        Assert.ThrowsAsync<GamenightException>(() => Action(changed, "eligibility", eligibility: true));
    }
    [Test, Description("Karaoke and staff attendees can order and keep catering charges despite free admission.")]
    public async Task FreeAdmissionDoesNotMakeMealsFree()
    {
        // Arrange
        var form = Form("karaoke@example.com");
        form.Attendance = "Karaoke";
        var karaoke = new Actor("karaoke", "karaoke@example.com", new HashSet<string>());
        await Service.SubmitAsync(new(Event.Id, form), karaoke, default);
        var registration = (await Service.DeskAsync(Admin, default)).Single(r => r.Form.Email == form.Email);
        // Act
        var order = await Catering.PlaceAsync(registration.Id,
            new(Event.Id, Menu.Version, Guid.Empty, [new(Pizza.Id, 1)]), karaoke, default);
        var staff = await Service.ActionAsync(Visitor.Id, new(Visitor.Version, "staff"), Admin, default);
        var staffOrder = await Order(new CateringQuantity(Pizza.Id, 1));
        // Assert
        Assert.That(registration.PriceCents, Is.Zero);
        Assert.That(staff.PriceCents, Is.Zero);
        Assert.That(order.OutstandingCents, Is.EqualTo(600));
        Assert.That(staffOrder.OutstandingCents, Is.EqualTo(600));
    }

}
