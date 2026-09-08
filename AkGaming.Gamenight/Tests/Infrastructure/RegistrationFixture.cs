using AkGaming.Gamenight.Application;
using AkGaming.Gamenight.Contracts;
using AkGaming.Gamenight.Infrastructure;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Moq;
using NUnit.Framework;

namespace AkGaming.Gamenight.Tests.Infrastructure;
public abstract class RegistrationFixture
{
    protected SqliteConnection Connection { get; private set; } = default!;
    protected GamenightDbContext Db { get; private set; } = default!;
    protected GamenightService Service { get; private set; } = default!;
    protected Mock<IMembershipClient> Membership { get; private set; } = default!;
    protected Mock<IGuestEmail> Email { get; private set; } = default!;
    protected EventSettings Event { get; private set; } = default!;
    protected static Actor Admin => new("admin", "admin@example.com", Permissions.All.ToHashSet());
    protected static Actor Guest => new(null, null, new HashSet<string>());
    protected static Actor Owner => new("owner", "visitor@example.com", new HashSet<string>());
    protected DateTime Now { get; } = new(2026, 9, 7, 12, 0, 0, DateTimeKind.Utc);
    protected string GuestToken { get; private set; } = "";
    [SetUp]
    public async Task Setup()
    {
        Connection = new SqliteConnection("Data Source=:memory:"); await Connection.OpenAsync();
        Db = NewContext();
        await Db.Database.MigrateAsync();
        Membership = new Mock<IMembershipClient>();
        Membership.Setup(m => m.PeriodsAsync(It.IsAny<CancellationToken>())).ReturnsAsync([new PaymentPeriodOption(1, "Winter 2026")]);
        Membership.Setup(m => m.GetAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync(new MembershipEligibility(false, "not-eligible"));
        Email = new Mock<IGuestEmail>();
        Email.Setup(m => m.Queue(It.IsAny<Domain.Registration>(), It.IsAny<string>(), It.IsAny<string>()))
            .Callback<Domain.Registration,string,string>((_, _, token) => GuestToken = token);
        Service = new GamenightService(Db, Membership.Object, Email.Object, new FixedClock(Now));
        Event = await Service.SaveEventAsync(new EventSettings { Name = "Test Night", StartsAt = Now.AddDays(5), EndsAt = Now.AddDays(6), SignupDeadline = Now.AddDays(4), EditDeadline = Now.AddDays(4), FoodDeadline = Now.AddDays(3), EarlyBirdDeadline = Now.AddDays(3), MembershipPaymentPeriodId = 1 }, Admin, default);
        var selection = await Service.ActiveAsync(default);
        await Service.ActivateAsync(new(Event.Id, selection.SelectionVersion), Admin, default);
    }
    protected GamenightDbContext NewContext() => new(new DbContextOptionsBuilder<GamenightDbContext>().UseSqlite(Connection, s => s.MigrationsAssembly("AkGaming.Gamenight.Migrations.Sqlite")).Options);
    protected static SignupForm Form(string email = "visitor@example.com") => new()
    {
        Email = email, FirstName = "Alex", LastName = "Visitor", Attendance = "GameNight",
        Sockets = 2, Meal = "Pizza", MealQuantity = 2, IceCream = "Ja", Scoops = 3, PenAndPaper = false,
        GameNightRules = true, GeneralRules = true, Liability = true, PhotoConsent = true,
        HardwareResponsibility = true, OrganizerDeclaration = false
    };
    protected async Task<RegistrationView> Register(Actor? actor = null)
    {
        await Service.SubmitAsync(new(Event.Id, Form()), actor ?? Guest, default);
        return (await Service.DeskAsync(Admin, default)).Single();
    }
    [TearDown]
    public async Task Cleanup() { await Db.DisposeAsync(); await Connection.DisposeAsync(); }
    private sealed class FixedClock(DateTime now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(now);
    }
}
