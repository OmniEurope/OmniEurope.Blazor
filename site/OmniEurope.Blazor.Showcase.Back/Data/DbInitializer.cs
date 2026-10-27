using Microsoft.EntityFrameworkCore;

namespace OmniEurope.Blazor.Showcase.Back.Data;

public static class DbInitializer
{
    /// <summary>
    /// Applies the pending migrations before the API answers, in every environment: the schema is the
    /// one of <c>Data/Migrations</c>, never one made up by <c>EnsureCreated</c>, so the development
    /// database and the deployed one cannot drift apart. There is no seed: visitors create their rows.
    /// </summary>
    public static async Task MigrateAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<ShowcaseDbContext>();
        await db.Database.MigrateAsync(cancellationToken).ConfigureAwait(false);
    }
}
