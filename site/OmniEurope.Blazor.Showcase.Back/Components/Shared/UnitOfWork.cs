using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
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
        catch (DbUpdateException exception) when (IsConcurrentWrite(exception))
        {
            throw new ConcurrentWriteException(exception);
        }
    }

    public async Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken) =>
        new Transaction(await db.Database.BeginTransactionAsync(cancellationToken).ConfigureAwait(false));

    public void Discard() => db.ChangeTracker.Clear();

    private static bool IsConcurrentWrite(DbUpdateException exception) =>
        exception is DbUpdateConcurrencyException
        || exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation };

    private sealed class Transaction(IDbContextTransaction transaction) : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken) => transaction.CommitAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
