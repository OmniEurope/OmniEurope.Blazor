using Npgsql;
using Testcontainers.PostgreSql;

[assembly: AssemblyFixture(typeof(OmniEurope.Blazor.Showcase.Back.Tests.Integration.PostgresFixture))]

namespace OmniEurope.Blazor.Showcase.Back.Tests.Integration;

/// <summary>
/// One real PostgreSQL for the whole run, started by Testcontainers (Docker required). Each test class
/// takes its own database from it, which the API's migrations create, so the classes never share rows.
/// </summary>
public sealed class PostgresFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _container = new PostgreSqlBuilder("postgres:18-alpine").Build();

    /// <summary>A connection string to a database of its own, not created yet (the migrations create it).</summary>
    public string NewDatabase() =>
        new NpgsqlConnectionStringBuilder(_container.GetConnectionString()) { Database = "t" + Guid.NewGuid().ToString("N") }.ConnectionString;

    public async ValueTask InitializeAsync() => await _container.StartAsync();

    public ValueTask DisposeAsync() => _container.DisposeAsync();
}
