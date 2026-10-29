namespace OmniEurope.Blazor.Showcase.Back.Components.Shared;

/// <summary>The changes the repositories staged in the request's context, saved or dropped as one.</summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves every staged change in one database transaction (the open <see cref="BeginTransactionAsync"/> one, when there is one).
    /// </summary>
    /// <exception cref="Exceptions.ConcurrentWriteException">
    /// Another request changed the same rows first (a concurrency token or a unique key): nothing was
    /// saved, and the caller may <see cref="Discard"/> and start again.
    /// </exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>
    /// Opens a database transaction that the reads, locks and saves of the request join until it ends:
    /// nothing it holds is kept unless <see cref="IUnitOfWorkTransaction.CommitAsync"/> is called, and
    /// disposing it without a commit rolls it back.
    /// </summary>
    Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken);

    /// <summary>Forgets every staged and loaded entity, so a retry reads the rows afresh.</summary>
    void Discard();
}

/// <summary>An open database transaction of the unit of work.</summary>
public interface IUnitOfWorkTransaction : IAsyncDisposable
{
    /// <summary>Keeps everything the transaction holds; a concurrent write was already raised by the save.</summary>
    Task CommitAsync(CancellationToken cancellationToken);
}
