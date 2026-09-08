using System.Text.Json;
using AkGaming.Management.Modules.MemberManagement.Domain.Entities;
using AkGaming.Management.Modules.MemberManagement.Domain.Enums;
using AkGaming.Management.Modules.MemberManagement.Infrastructure.Persistence;
using AkGaming.Management.WebApi.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using NUnit.Framework;

namespace AkGaming.Gamenight.Tests.Infrastructure;
public sealed class ManagementMembershipTests
{
    private SqliteConnection Connection { get; set; } = default!;
    private MemberManagementDbContext Db { get; set; } = default!;
    private GamenightMembershipController Controller { get; set; } = default!;
    private Guid UserId { get; set; }
    private Member Member { get; set; } = default!;
    [SetUp]
    public async Task Setup()
    {
        Connection = new SqliteConnection("Data Source=:memory:");
        await Connection.OpenAsync();
        Db = new(new DbContextOptionsBuilder<MemberManagementDbContext>().UseSqlite(Connection).Options);
        await Db.Database.EnsureCreatedAsync();
        UserId = Guid.NewGuid();
        Member = new Member { UserId = UserId, Status = MembershipStatus.Member };
        Db.Members.Add(Member);
        Db.MembershipPaymentPeriods.AddRange(
            new MembershipPaymentPeriod { Id = 1, Name = "Earlier", DueDate = new DateOnly(2026, 4, 1) },
            new MembershipPaymentPeriod { Id = 2, Name = "Current", DueDate = new DateOnly(2026, 10, 1) });
        await Db.SaveChangesAsync();
        Controller = new(Db);
    }
    [TearDown]
    public async Task Cleanup() { await Db.DisposeAsync(); await Connection.DisposeAsync(); }

    [TestCase(MembershipDueStatus.Paid, true)]
    [TestCase(MembershipDueStatus.Pending, false)]
    [TestCase(MembershipDueStatus.Waived, false)]
    [Description("SQLite membership eligibility uses the selected payment period and an actually paid contribution.")]
    public async Task SelectedPeriodMustBePaid(MembershipDueStatus status, bool expected)
    {
        // Arrange
        Db.MembershipDues.Add(new MembershipDue { MemberId = Member.Id, PaymentPeriodId = 2, Status = status, DueAmount = 15.50m });
        await Db.SaveChangesAsync();
        // Act
        var response = (OkObjectResult)await Controller.Eligibility(UserId, 2, default);
        var json = JsonSerializer.SerializeToElement(response.Value);
        // Assert
        Assert.That(json.GetProperty("Eligible").GetBoolean(), Is.EqualTo(expected));
    }
    [Test, Description("A paid older contribution does not qualify a member for the selected current period.")]
    public async Task OlderPaymentDoesNotQualify()
    {
        // Arrange
        Db.MembershipDues.Add(new MembershipDue { MemberId = Member.Id, PaymentPeriodId = 1, Status = MembershipDueStatus.Paid });
        await Db.SaveChangesAsync();
        // Act
        var response = (OkObjectResult)await Controller.Eligibility(UserId, 2, default);
        // Assert
        Assert.That(JsonSerializer.SerializeToElement(response.Value).GetProperty("Eligible").GetBoolean(), Is.False);
    }
    [Test, Description("A suspended member does not qualify even with a paid contribution.")]
    public async Task SuspendedMemberDoesNotQualify()
    {
        // Arrange
        Member.Status = MembershipStatus.Suspended;
        Db.MembershipDues.Add(new MembershipDue { MemberId = Member.Id, PaymentPeriodId = 2, Status = MembershipDueStatus.Paid });
        await Db.SaveChangesAsync();
        // Act
        var response = (OkObjectResult)await Controller.Eligibility(UserId, 2, default);
        // Assert
        Assert.That(JsonSerializer.SerializeToElement(response.Value).GetProperty("Eligible").GetBoolean(), Is.False);
    }
    [Test, Description("The payment-period selector orders periods by due date using a provider-backed SQLite query.")]
    public async Task PeriodsAreNewestFirst()
    {
        // Arrange
        // Periods are seeded in ascending order by the fixture.
        // Act
        var response = (OkObjectResult)await Controller.Periods(default);
        // Assert
        Assert.That(JsonSerializer.SerializeToElement(response.Value)[0].GetProperty("Id").GetInt32(), Is.EqualTo(2));
    }
}
