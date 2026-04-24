using AkGaming.Tournaments.Infrastructure.Sqlite.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AkGaming.Tournaments.Tests.Infrastructure;

internal sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private bool _disposed;

    public SqliteTestDatabase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var dbContext = CreateContext();
        dbContext.Database.EnsureCreated();
    }

    public TournamentDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<TournamentDbContext>()
            .UseSqlite(_connection)
            .EnableSensitiveDataLogging()
            .Options;

        return new TournamentDbContext(options);
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _connection.Dispose();
        _disposed = true;
    }
}
