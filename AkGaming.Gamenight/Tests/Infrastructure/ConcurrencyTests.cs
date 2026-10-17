using AkGaming.Gamenight.Application;
using AkGaming.Gamenight.Contracts;
using NUnit.Framework;
namespace AkGaming.Gamenight.Tests.Infrastructure;
public sealed class ConcurrencyTests : RegistrationFixture
{
    [Test, Description("SQLite optimistic concurrency rejects a second desk update based on stale registration data.")]
    public async Task ConcurrentDeskUpdateIsRejected()
    {
        // Arrange
        var r = await Register();
        await using var other = NewContext();
        var stale = await other.RegistrationAsync(r.Id, default);
        // Act
        await Service.ActionAsync(r.Id, new(r.Version, "staff"), Admin, default);
        stale!.Paid = true; stale.Version = Guid.NewGuid();
        // Assert
        Assert.ThrowsAsync<GamenightException>(() => other.SaveAsync(default));
    }
    [Test, Description("Switching events hides old registrations and rejects submissions from an already open old form.")]
    public async Task ActiveEventSeparatesRegistrations()
    {
        // Arrange
        await Register();
        var next = await Service.SaveEventAsync(new EventSettings { Name = "Next Night", MembershipPaymentPeriodId = 1 }, Admin, default);
        var selection = await Service.ActiveAsync(default);
        // Act
        await Service.ActivateAsync(new(next.Id, selection.SelectionVersion), Admin, default);
        var desk = await Service.DeskAsync(Admin, default);
        // Assert
        Assert.That(desk, Is.Empty);
        Assert.That(Db.Registrations.Count(), Is.EqualTo(1));
        Assert.ThrowsAsync<GamenightException>(() => Service.SubmitAsync(new(Event.Id, Form("other@example.com")), Guest, default));
    }
    [Test, Description("The global selection concurrency token prevents a write committing after an event switch.")]
    public async Task EventSwitchConflictsWithPendingWrite()
    {
        // Arrange
        await using var other = NewContext();
        var selection = await other.SelectionAsync(default);
        selection.Version = Guid.NewGuid();
        var current = await Service.ActiveAsync(default);
        // Act
        await Service.ActivateAsync(new(Event.Id, current.SelectionVersion), Admin, default);
        // Assert
        Assert.ThrowsAsync<GamenightException>(() => other.SaveAsync(default));
    }
}
