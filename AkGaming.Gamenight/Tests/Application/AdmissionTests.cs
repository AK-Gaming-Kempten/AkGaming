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
        Assert.That(r.PriceCents, Is.EqualTo(Event.MemberEarlyPriceCents));
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
}

