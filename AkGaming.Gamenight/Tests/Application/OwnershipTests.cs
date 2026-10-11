using AkGaming.Gamenight.Application;
using AkGaming.Gamenight.Contracts;
using AkGaming.Gamenight.Tests.Infrastructure;
using NUnit.Framework;
namespace AkGaming.Gamenight.Tests.Application;
public sealed class OwnershipTests : RegistrationFixture
{
    [Test, Description("A verified matching email claims the guest signup, invalidates its link and permits editing.")]
    public async Task VerifiedEmailClaimsRegistration()
    {
        // Arrange
        var registration = await Register();
        // Act
        var mine = (await Service.MineAsync(Owner, default)).Single();
        // Assert
        Assert.That(mine.CanEdit, Is.True);
        Assert.That(mine.Id, Is.EqualTo(registration.Id));
        Assert.ThrowsAsync<GamenightException>(() => Service.GuestAsync(registration.Id, GuestToken, default));
    }
    [Test, Description("An unverified or unrelated account cannot claim or edit a guest registration.")]
    public async Task UnverifiedEmailCannotClaim()
    {
        // Arrange
        var r = await Register();
        var unverified = new Actor("owner", null, new HashSet<string>());
        // Act
        var rows = await Service.MineAsync(unverified, default);
        // Assert
        Assert.That(rows, Is.Empty);
        Assert.ThrowsAsync<GamenightException>(() => Service.UpdateAsync(r.Id, new(r.Version, Form()), unverified, default));
    }
    [Test, Description("The private guest link can view but cannot edit the registration.")]
    public async Task GuestLinkIsReadOnly()
    {
        // Arrange
        var r = await Register();
        // Act
        var guest = await Service.GuestAsync(r.Id, GuestToken, default);
        // Assert
        Assert.That(guest.CanEdit, Is.False);
        Assert.That(guest.Form.Email, Is.EqualTo("visitor@example.com"));
        Assert.ThrowsAsync<GamenightException>(() => Service.ActionAsync(r.Id, new(r.Version, "cancel"), Guest, default));
    }
    [Test, Description("Case and whitespace variations of an existing email are rejected without creating another registration.")]
    public async Task DuplicateEmailIsIdempotent()
    {
        // Arrange
        await Register();
        // Act
        var error = Assert.ThrowsAsync<GamenightException>(() => Service.SubmitAsync(new(Event.Id, Form(" VISITOR@example.com ")), Guest, default));
        // Assert
        Assert.That(Db.Registrations.Count(), Is.EqualTo(1));
    }
    [Test, Description("After claiming, changing the account email does not transfer the registration to another account.")]
    public async Task OwnershipUsesStableSubject()
    {
        // Arrange
        await Register();
        await Service.MineAsync(Owner, default);
        // Act
        var original = await Service.MineAsync(Owner with { VerifiedEmail = "changed@example.com" }, default);
        var other = await Service.MineAsync(Owner with { Id = "other" }, default);
        // Assert
        Assert.That(original, Has.Count.EqualTo(1));
        Assert.That(other, Is.Empty);
    }
    [Test, Description("A duplicate submission by its owner reports a conflict rather than claiming a second signup was processed.")]
    public async Task OwnerDuplicateReportsConflict()
    {
        // Arrange
        await Register(Owner);
        var form = Form();
        form.FirstName = "Second";
        // Act
        var error = Assert.ThrowsAsync<GamenightException>(() => Service.SubmitAsync(new(Event.Id, form), Owner, default));
        // Assert
        Assert.That(error!.Message, Does.Contain("keine weitere Person"));
        Assert.That(Db.Registrations.Count(), Is.EqualTo(1));
    }

    [Test, Description("A second person with a different email is saved and visible at the front desk without assigning someone else's email to the submitting account.")]
    public async Task SecondEmailIsVisibleAtDesk()
    {
        // Arrange
        await Register(Owner);
        var form = Form("second@example.com");
        form.FirstName = "Second";
        // Act
        await Service.SubmitAsync(new(Event.Id, form), Owner, default);
        var desk = await Service.DeskAsync(Admin, default);
        var mine = await Service.MineAsync(Owner, default);
        // Assert
        Assert.That(desk, Has.Count.EqualTo(2));
        Assert.That(desk.Any(row => row.Form.Email == "second@example.com"), Is.True);
        Assert.That(mine, Has.Count.EqualTo(1));
    }
    [Test, Description("The first-step email check rejects existing emails but accepts a new email without saving a registration.")]
    public async Task EmailCheckValidatesWithoutSaving()
    {
        // Arrange
        await Register();
        // Act
        var error = Assert.ThrowsAsync<GamenightException>(() => Service.ValidateSignupEmailAsync(new(Event.Id, " VISITOR@example.com "), default));
        await Service.ValidateSignupEmailAsync(new(Event.Id, "new@example.com"), default);
        // Assert
        Assert.That(error!.Status, Is.EqualTo(409));
        Assert.That(Db.Registrations.Count(), Is.EqualTo(1));
    }
}

