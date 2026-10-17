using AkGaming.Gamenight.Application;
using AkGaming.Gamenight.Contracts;
using AkGaming.Gamenight.Tests.Infrastructure;
using Moq;
using NUnit.Framework;
namespace AkGaming.Gamenight.Tests.Application;
public sealed class AdmissionTests : RegistrationFixture
{
    [Test, Description("A paid Management membership gives the member rate even when a setup is requested.")]
    public async Task MemberPricingComesFromManagement()
    {
        // Arrange
        Membership.Setup(m => m.GetAsync("owner", 1, It.IsAny<CancellationToken>())).ReturnsAsync(new MembershipEligibility(true, "paid"));
        // Act
        var r = await Register(Owner);
        // Assert
        Assert.That(r.PriceCents, Is.EqualTo(Event.MemberPrice(true, true)));
        Assert.That(r.MemberEligible, Is.True);
        Assert.That(r.PriceWithoutSetupCents, Is.EqualTo(Event.MemberPrice(true, false)));
        Assert.That(r.PriceWithSetupCents, Is.EqualTo(Event.MemberPrice(true, true)));
    }
    [Test, Description("Organizer self-declaration does not grant free admission; authorized staff approval does.")]
    public async Task OnlyAdminApprovalGrantsStaffEntry()
    {
        // Arrange
        var form = Form(); form.OrganizerDeclaration = true;
        await Service.SubmitAsync(new(Event.Id, form), Guest, default);
        var r = (await Service.DeskAsync(Admin, default)).Single();
        // Act
        var approved = await Service.ActionAsync(r.Id, new(r.Version, "staff"), Admin, default);
        // Assert
        Assert.That(r.PriceCents, Is.EqualTo(700));
        Assert.That(approved.PriceCents, Is.Zero);
    }
    [Test, Description("Check-in requires admission payment and locks visitor edits and cancellation afterwards.")]
    public async Task CheckinLocksVisitorChanges()
    {
        // Arrange
        var r = await Register(Owner);
        // Act
        Assert.ThrowsAsync<GamenightException>(() => Service.ActionAsync(r.Id, new(r.Version, "checkin"), Admin, default));
        var paid = await Service.ActionAsync(r.Id, new(r.Version, "pay"), Admin, default);
        var checkedIn = await Service.ActionAsync(r.Id, new(paid.Version, "checkin"), Admin, default);
        // Assert
        Assert.ThrowsAsync<GamenightException>(() => Service.UpdateAsync(r.Id, new(checkedIn.Version, Form()), Owner, default));
        Assert.ThrowsAsync<GamenightException>(() => Service.ActionAsync(r.Id, new(checkedIn.Version, "cancel"), Owner, default));
    }
    [Test, Description("A Management outage leaves pricing unresolved and prevents collection of an unverified price.")]
    public async Task MembershipOutageDoesNotSilentlyChargeFullPrice()
    {
        // Arrange
        Membership.Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ThrowsAsync(new HttpRequestException());
        // Act
        var r = await Register(Owner);
        // Assert
        Assert.That(r.PriceCents, Is.Null);
        Assert.ThrowsAsync<GamenightException>(() => Service.ActionAsync(r.Id, new(r.Version, "pay"), Admin, default));
    }
    [Test, Description("Karaoke-only signup clears hidden Game Night answers and has free admission.")]
    public async Task KaraokeHasNoGameNightRequirements()
    {
        // Arrange
        var form = Form(); form.Attendance = "Karaoke"; form.GameNightRules = false; form.PenAndPaper = null;
        // Act
        await Service.SubmitAsync(new(Event.Id, form), Guest, default);
        var r = (await Service.DeskAsync(Admin, default)).Single();
        // Assert
        Assert.That(r.PriceCents, Is.Zero);
        Assert.That(r.Form.Meal, Is.Null);
    }
    [Test, Description("A user without admission permission cannot approve free staff entry.")]
    public async Task OwnerCannotApproveStaff()
    {
        // Arrange
        var r = await Register(Owner);
        // Act
        var error = Assert.ThrowsAsync<GamenightException>(() => Service.ActionAsync(r.Id, new(r.Version, "staff"), Owner, default));
        // Assert
        Assert.That(error!.Status, Is.EqualTo(403));
    }
    [Test, Description("The overview exposes both early-bird setup tariffs while retaining the selected payable amount.")]
    public async Task OverviewShowsBothSetupPrices()
    {
        // Arrange
        var form = Form();
        form.Sockets = 0;
        // Act
        await Service.SubmitAsync(new(Event.Id, form), Guest, default);
        var registration = (await Service.DeskAsync(Admin, default)).Single();
        // Assert
        Assert.That(registration.PriceWithoutSetupCents, Is.EqualTo(Event.EarlyPriceCents));
        Assert.That(registration.PriceWithSetupCents, Is.EqualTo(Event.EarlySetupPriceCents));
        Assert.That(registration.PriceCents, Is.EqualTo(Event.EarlyPriceCents));
    }
    [TestCase(true, 0, 100), TestCase(true, 2, 150), TestCase(false, 0, 200), TestCase(false, 2, 250)]
    [Description("Each of the four member discounts is deducted from its matching standard tariff.")]
    public async Task MemberDiscountMatchesCategory(bool early, int sockets, int discount)
    {
        // Arrange
        Event.MemberEarlyDiscountCents = 100;
        Event.MemberEarlySetupDiscountCents = 150;
        Event.MemberRegularDiscountCents = 200;
        Event.MemberRegularSetupDiscountCents = 250;
        Event.EarlyBirdDeadline = early ? Now.AddDays(1) : Now.AddDays(-1);
        await Service.SaveEventAsync(Event, Admin, default);
        Membership.Setup(m => m.GetAsync("owner", 1, It.IsAny<CancellationToken>())).ReturnsAsync(new MembershipEligibility(true, "paid"));
        var form = Form();
        form.Sockets = sockets;
        var standard = early ? (sockets > 0 ? Event.EarlySetupPriceCents : Event.EarlyPriceCents) : (sockets > 0 ? Event.RegularSetupPriceCents : Event.RegularPriceCents);
        // Act
        await Service.SubmitAsync(new(Event.Id, form), Owner, default);
        var registration = (await Service.MineAsync(Owner, default)).Single();
        // Assert
        Assert.That(registration.PriceCents, Is.EqualTo(standard - discount));
        Assert.That(registration.PriceWithoutSetupCents, Is.EqualTo(Event.MemberPrice(early, false)));
        Assert.That(registration.PriceWithSetupCents, Is.EqualTo(Event.MemberPrice(early, true)));
    }
    [Test, Description("Legacy fixed member prices are read as equivalent discounts for both setup categories.")]
    public async Task LegacyMemberPricesBecomeDiscounts()
    {
        // Arrange
        var entity = await Db.EventAsync(Event.Id, default);
        var json = System.Text.Json.Nodes.JsonNode.Parse(entity!.SettingsJson)!;
        foreach (var key in new[] { "MemberEarlyDiscountCents", "MemberEarlySetupDiscountCents", "MemberRegularDiscountCents", "MemberRegularSetupDiscountCents" })
            json.AsObject().Remove(key);
        json["MemberEarlyPriceCents"] = 300;
        json["MemberRegularPriceCents"] = 400;
        entity.SettingsJson = json.ToJsonString();
        await Db.SaveAsync(default);
        // Act
        var settings = (await Service.ActiveAsync(default)).Event!;
        // Assert
        Assert.That(settings.MemberPrice(true, false), Is.EqualTo(300));
        Assert.That(settings.MemberPrice(true, true), Is.EqualTo(300));
        Assert.That(settings.MemberPrice(false, false), Is.EqualTo(400));
        Assert.That(settings.MemberPrice(false, true), Is.EqualTo(400));
    }
}

