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
        Assert.That(r.PriceCents, Is.EqualTo(Event.MemberPrice(true)));
        Assert.That(r.MemberEligible, Is.True);
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
        Assert.That(r.PriceCents, Is.EqualTo(Event.EarlyPriceCents));
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
    [TestCase(0), TestCase(1), TestCase(2), TestCase(4)]
    [Description("The admission price is identical for all setup requirements.")]
    public async Task SetupDoesNotChangeAdmissionPrice(int sockets)
    {
        // Arrange
        var form = Form();
        form.Sockets = sockets;
        // Act
        await Service.SubmitAsync(new(Event.Id, form), Guest, default);
        var registration = (await Service.DeskAsync(Admin, default)).Single();
        // Assert
        Assert.That(registration.PriceCents, Is.EqualTo(Event.EarlyPriceCents));
    }
    [TestCase(true, 0, 100), TestCase(true, 2, 100), TestCase(false, 0, 200), TestCase(false, 2, 200)]
    [Description("The booking-period member discount applies equally with or without a setup.")]
    public async Task MemberDiscountMatchesCategory(bool early, int sockets, int discount)
    {
        // Arrange
        Event.MemberEarlyDiscountCents = 100;
        Event.MemberRegularDiscountCents = 200;
        Event.EarlyBirdDeadline = early ? Now.AddDays(1) : Now.AddDays(-1);
        await Service.SaveEventAsync(Event, Admin, default);
        Membership.Setup(m => m.GetAsync("owner", 1, It.IsAny<CancellationToken>())).ReturnsAsync(new MembershipEligibility(true, "paid"));
        var form = Form();
        form.Sockets = sockets;
        var standard = early ? Event.EarlyPriceCents : Event.RegularPriceCents;
        // Act
        await Service.SubmitAsync(new(Event.Id, form), Owner, default);
        var registration = (await Service.MineAsync(Owner, default)).Single();
        // Assert
        Assert.That(registration.PriceCents, Is.EqualTo(standard - discount));
    }
    [Test, Description("Legacy fixed member prices are read as equivalent discounts for each booking period.")]
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
        Assert.That(settings.MemberPrice(true), Is.EqualTo(300));
        Assert.That(settings.MemberPrice(false), Is.EqualTo(400));
    }
    [Test, Description("Removing setup tariffs preserves previously settled admission amounts.")]
    public async Task ExistingPaidSetupPriceRemainsSettled()
    {
        // Arrange
        var registration = await Register(Owner);
        var entity = await Db.RegistrationAsync(registration.Id, default);
        entity!.Paid = true;
        entity.SettledPriceCents = 700;
        entity.SettledAdmission = "Frühbucher";
        await Db.SaveAsync(default);
        // Act
        var view = (await Service.MineAsync(Owner, default)).Single();
        // Assert
        Assert.That(view.Paid, Is.True);
        Assert.That(view.PriceCents, Is.EqualTo(700));
    }
    [Test, Description("Changing setup requirements after payment does not change the settled admission price.")]
    public async Task SetupCanChangeWithoutReopeningPayment()
    {
        // Arrange
        var registration = await Register(Owner);
        var paid = await Service.ActionAsync(registration.Id, new(registration.Version, "pay"), Admin, default);
        var form = Form();
        form.Sockets = 4;
        // Act
        var updated = await Service.UpdateAsync(registration.Id, new(paid.Version, form), Owner, default);
        // Assert
        Assert.That(updated.Form.Sockets, Is.EqualTo(4));
        Assert.That(updated.Paid, Is.True);
        Assert.That(updated.PriceCents, Is.EqualTo(paid.PriceCents));
    }

}

