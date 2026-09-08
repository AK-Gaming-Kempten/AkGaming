using AkGaming.Gamenight.Contracts;
using AkGaming.Gamenight.Domain;
namespace AkGaming.Gamenight.Application;

public sealed record Actor(string? Id, string? VerifiedEmail, IReadOnlySet<string> Permissions)
{
    public bool Has(string permission) => Id is not null && Permissions.Contains(permission);
}
public interface IGamenightStore
{
    Task<EventSelection> SelectionAsync(CancellationToken ct);
    Task<GamenightEvent?> EventAsync(Guid id, CancellationToken ct);
    Task<List<GamenightEvent>> EventsAsync(CancellationToken ct);
    Task<Registration?> RegistrationAsync(Guid id, CancellationToken ct);
    Task<List<Registration>> RegistrationsAsync(Guid eventId, CancellationToken ct);
    void Add(object entity);
    Task SaveAsync(CancellationToken ct);
}
public interface IMembershipClient
{
    Task<MembershipEligibility> GetAsync(string userId, int periodId, CancellationToken ct);
    Task<List<PaymentPeriodOption>> PeriodsAsync(CancellationToken ct);
}
public interface IGuestEmail
{
    void Queue(Registration registration, string email, string token);
}
public sealed class GamenightException(int status, string message) : Exception(message)
{
    public int Status { get; } = status;
}
public interface IGamenightService
{
    Task<ActiveEvent> ActiveAsync(CancellationToken ct);
    Task<List<EventSettings>> EventsAsync(Actor actor, CancellationToken ct);
    Task<EventSettings> SaveEventAsync(EventSettings settings, Actor actor, CancellationToken ct);
    Task ActivateAsync(ActivateEvent request, Actor actor, CancellationToken ct);
    Task SubmitAsync(SubmitSignup request, Actor actor, CancellationToken ct);
    Task<List<RegistrationView>> MineAsync(Actor actor, CancellationToken ct);
    Task<RegistrationView> GuestAsync(Guid id, string token, CancellationToken ct);
    Task<List<RegistrationView>> DeskAsync(Actor actor, CancellationToken ct);
    Task<RegistrationView> UpdateAsync(Guid id, UpdateSignup request, Actor actor, CancellationToken ct);
    Task<RegistrationView> ActionAsync(Guid id, DeskAction request, Actor actor, CancellationToken ct);
}

