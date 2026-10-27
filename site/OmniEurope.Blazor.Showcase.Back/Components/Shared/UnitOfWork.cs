using Microsoft.EntityFrameworkCore;
using Npgsql;
using OmniEurope.Blazor.Showcase.Back.Data;
using OmniEurope.Blazor.Showcase.Back.Exceptions;

namespace OmniEurope.Blazor.Showcase.Back.Components.Shared;

public sealed class UnitOfWork(ShowcaseDbContext db) : IUnitOfWork
{
    public async Task SaveChangesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException exception)
        {
            throw new ConcurrentWriteException(exception);
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            throw new ConcurrentWriteException(exception);
        }
    }

    public void Discard() => db.ChangeTracker.Clear();
}
