namespace OmniEurope.Blazor.Components;

/// <summary>
/// Persists an <see cref="OmniDataGrid{TItem}"/>'s filter, sort and column-width state under an
/// opaque key, and reads it back. The package ships <see cref="OmniLocalStorageDataGridStateStore"/>
/// as the default (browser storage, per device); a host application can instead register its own
/// implementation of this same interface, for example one backed by a database table keyed on the
/// signed-in user, with no change required to <see cref="OmniDataGrid{TItem}"/> itself.
/// </summary>
public interface IOmniDataGridStateStore
{
    /// <summary>Returns the previously saved state for <paramref name="key"/>, or <c>null</c> if none exists.</summary>
    Task<string?> LoadAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Overwrites the saved state for <paramref name="key"/>.</summary>
    Task SaveAsync(string key, string state, CancellationToken cancellationToken = default);
}
