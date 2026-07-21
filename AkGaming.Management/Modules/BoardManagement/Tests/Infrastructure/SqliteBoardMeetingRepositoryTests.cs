using AkGaming.Management.Modules.BoardManagement.Domain.Entities;
using AkGaming.Management.Modules.BoardManagement.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Management.Modules.BoardManagement.Tests.Infrastructure;

[TestFixture]
public sealed class SqliteBoardMeetingRepositoryTests
{
    private SqliteConnection _connection = null!;
    private BoardManagementDbContext _dbContext = null!;
    private EfBoardMeetingRepository _repository = null!;

    [SetUp]
    public async Task SetUp()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        await _connection.OpenAsync();
        var options = new DbContextOptionsBuilder<BoardManagementDbContext>()
            .UseSqlite(_connection)
            .Options;
        _dbContext = new BoardManagementDbContext(options);
        await _dbContext.Database.EnsureCreatedAsync();
        _repository = new EfBoardMeetingRepository(_dbContext);
    }

    [TearDown]
    public async Task TearDown()
    {
        await _dbContext.DisposeAsync();
        await _connection.DisposeAsync();
    }

    [Test]
    [Description("Loads and orders backlog items with DateTimeOffset values when using the SQLite provider.")]
    public async Task GetBacklogAsync_WithDateTimeOffsetOrdering_ReturnsOrderedItems()
    {
        // Arrange
        var later = new BoardAgendaItem
        {
            Title = "Later",
            Order = 2,
            CreatedAtUtc = new DateTimeOffset(2026, 7, 21, 12, 0, 0, TimeSpan.Zero),
            UpdatedAtUtc = new DateTimeOffset(2026, 7, 21, 12, 0, 0, TimeSpan.Zero)
        };
        var secondAtSameOrder = new BoardAgendaItem
        {
            Title = "Second",
            Order = 1,
            CreatedAtUtc = new DateTimeOffset(2026, 7, 21, 11, 0, 0, TimeSpan.Zero),
            UpdatedAtUtc = new DateTimeOffset(2026, 7, 21, 11, 0, 0, TimeSpan.Zero)
        };
        var firstAtSameOrder = new BoardAgendaItem
        {
            Title = "First",
            Order = 1,
            CreatedAtUtc = new DateTimeOffset(2026, 7, 21, 10, 0, 0, TimeSpan.Zero),
            UpdatedAtUtc = new DateTimeOffset(2026, 7, 21, 10, 0, 0, TimeSpan.Zero)
        };
        _dbContext.AgendaItems.AddRange(later, secondAtSameOrder, firstAtSameOrder);
        await _dbContext.SaveChangesAsync();

        // Act
        var result = await _repository.GetBacklogAsync(CancellationToken.None);

        // Assert
        Assert.That(result.Select(x => x.Title), Is.EqualTo(new[] { "First", "Second", "Later" }));
    }

}
