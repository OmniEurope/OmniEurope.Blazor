using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;

namespace OmniEurope.Blazor.Showcase.Back.Tests.Integration;

/// <summary>
/// The real API (its <c>Program</c>, middleware, controllers, EF Core migrations) over a database of its
/// own on the shared PostgreSQL. It runs as in production (<c>Production</c> environment), so an error
/// response is the one a visitor would get. Settings are given as the environment of a container would.
/// </summary>
public sealed class ShowcaseApiFactory(PostgresFixture postgres, IReadOnlyDictionary<string, string>? settings = null)
    : WebApplicationFactory<Program>
{
    public const string FrontOrigin = "https://showcase.example";

    public string ConnectionString { get; } = postgres.NewDatabase();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Production");
        builder.UseSetting("ConnectionStrings:DefaultConnection", ConnectionString);
        builder.UseSetting("Cors:Origins:0", FrontOrigin);
        foreach (var (key, value) in settings ?? new Dictionary<string, string>())
        {
            builder.UseSetting(key, value);
        }
    }
}
