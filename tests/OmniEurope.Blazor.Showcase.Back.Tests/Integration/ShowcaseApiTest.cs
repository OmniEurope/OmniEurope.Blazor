namespace OmniEurope.Blazor.Showcase.Back.Tests.Integration;

/// <summary>
/// An HTTP test of the API over the shared PostgreSQL: each test gets a fresh application, with a database of
/// its own, and a client to it, both disposed once the test is over.
/// </summary>
public abstract class ShowcaseApiTest(PostgresFixture postgres) : IAsyncLifetime
{
    protected ShowcaseApiFactory Factory { get; private set; } = null!;

    protected HttpClient Client { get; private set; } = null!;

    protected static CancellationToken Token => TestContext.Current.CancellationToken;

    public ValueTask InitializeAsync()
    {
        Factory = new ShowcaseApiFactory(postgres);
        Client = Factory.CreateClient();
        return ValueTask.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await Factory.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}
