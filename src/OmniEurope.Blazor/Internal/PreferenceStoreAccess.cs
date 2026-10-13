using Microsoft.Extensions.Logging;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Internal;

/// <summary>
/// The calls of <see cref="OmniPreferences"/> to the host's <see cref="IOmniPreferenceStore"/>, one at a time
/// (the store is scoped, often over a <c>DbContext</c>, and a synchronization runs off the renderer's
/// thread while the UI writes), with the reads and writes whose failure is logged rather than thrown.
/// </summary>
internal sealed class PreferenceStoreAccess(IOmniPreferenceStore store, ILogger logger)
{
    private readonly SemaphoreSlim _gate = new(1, 1);

    /// <summary>One call to the store, never at the same time as another.</summary>
    internal async Task<T> CallAsync<T>(Func<IOmniPreferenceStore, Task<T>> call, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            return await call(store);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <summary>The stored preference, or null when the store has none or fails (logged: the browser cache answers instead).</summary>
    internal async Task<OmniPreference?> TryGetAsync(string key, CancellationToken cancellationToken)
    {
        try
        {
            return await CallAsync(current => current.GetAsync(key, cancellationToken), cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "The preference {Key} could not be read from the store; the browser cache answers instead.", key);
            return null;
        }
    }

    /// <summary>
    /// Saves a preference while <paramref name="stillCurrent"/>, checked under the gate, says it is still the
    /// one to save: a write that replaced it is saved after it, by its own call, so an older value is never
    /// saved over a newer one. False when it was not current or the store failed (logged).
    /// </summary>
    internal async Task<bool> TrySaveAsync(OmniPreference preference, Func<Task<bool>> stillCurrent, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!await stillCurrent())
            {
                return false;
            }

            await store.SaveAsync(preference, cancellationToken);
            return true;
        }
        catch (Exception exception) when (exception is not OperationCanceledException || !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(exception, "The preference {Key} could not be written to the store; it stays in the browser and the next synchronization writes it again.", preference.Key);
            return false;
        }
        finally
        {
            _gate.Release();
        }
    }
}

