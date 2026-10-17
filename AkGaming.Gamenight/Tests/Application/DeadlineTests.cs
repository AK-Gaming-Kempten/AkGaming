using AkGaming.Gamenight.Application;
using AkGaming.Gamenight.Tests.Infrastructure;
using NUnit.Framework;
namespace AkGaming.Gamenight.Tests.Application;
public sealed class DeadlineTests : RegistrationFixture
{
    [Test, Description("Self-service editing and cancellation stop at the configured deadline before check-in.")]
    public async Task EditDeadlineIsEnforced()
    {
        // Arrange
        var r = await Register(Owner);
        Event.EditDeadline = Now.AddMinutes(-1);
        await Service.SaveEventAsync(Event, Admin, default);
        // Act
        var mine = (await Service.MineAsync(Owner, default)).Single();
        // Assert
        Assert.That(mine.CanEdit, Is.False);
        Assert.ThrowsAsync<GamenightException>(() => Service.ActionAsync(r.Id, new(r.Version, "cancel"), Owner, default));
    }
    [Test, Description("Expired signup deadlines are enforced on the server.")]
    public async Task SignupDeadlineIsEnforced()
    {
        // Arrange
        Event.SignupDeadline = Now.AddMinutes(-1);
        await Service.SaveEventAsync(Event, Admin, default);
        // Act
        var error = Assert.ThrowsAsync<GamenightException>(() => Service.SubmitAsync(new(Event.Id, Form()), Guest, default));
        // Assert
        Assert.That(error!.Status, Is.EqualTo(409));
    }
}
