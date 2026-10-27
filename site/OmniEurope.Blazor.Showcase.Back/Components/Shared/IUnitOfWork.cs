namespace OmniEurope.Blazor.Showcase.Back.Components.Shared;

/// <summary>The changes the repositories staged in the request's context, saved or dropped as one.</summary>
public interface IUnitOfWork
{
    /// <summary>
    /// Saves every staged change in one database transaction.
    /// </summary>
    /// <exception cref="Exceptions.ConcurrentWriteException">
    /// Another request changed the same rows first (a concurrency token or a unique key): nothing was
    /// saved, and the caller may <see cref="Discard"/> and start again.
    /// </exception>
    Task SaveChangesAsync(CancellationToken cancellationToken);

    /// <summary>Forgets every staged and loaded entity, so a retry reads the rows afresh.</summary>
    void Discard();
}
