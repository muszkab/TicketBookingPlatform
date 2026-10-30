using Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using System;

namespace Infrastructure.Tests.Common;

/// <summary>
/// SQLite in-memory database that keeps the underlying connection open for the
/// lifetime of the test, so real relational behaviour (unique indexes, FK
/// constraints, column types) can be asserted.
/// </summary>
internal sealed class SqliteTestDatabase : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<ApplicationDbContext> _options;

    private SqliteTestDatabase(SqliteConnection connection, DbContextOptions<ApplicationDbContext> options)
    {
        _connection = connection;
        _options = options;
    }

    public static SqliteTestDatabase Create()
    {
        var connection = new SqliteConnection("DataSource=:memory:");
        connection.Open();

        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlite(connection)
            .Options;

        using (var context = new ApplicationDbContext(options))
        {
            context.Database.EnsureCreated();
        }

        return new SqliteTestDatabase(connection, options);
    }

    public ApplicationDbContext CreateContext() => new(_options);

    public void Dispose() => _connection.Dispose();
}

internal static class TestDbContextFactory
{
    /// <summary>
    /// A context that is never connected to a provider-specific database but whose
    /// model is fully built, useful for pure mapping/metadata assertions.
    /// </summary>
    public static ApplicationDbContext CreateForModelInspection()
    {
        DbContextOptions<ApplicationDbContext> options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseSqlServer("Server=(localdb)\\mssqllocaldb;Database=ModelOnly")
            .Options;

        return new ApplicationDbContext(options);
    }
}
