
namespace OmniEurope.Blazor.Components;

/// <summary>
/// What a <see cref="PreferenceReads"/> calls back into on the preferences service it reads for: whether a
/// store is registered, the tracking of background work, the running of store work under the service's
/// lifetime, and the raising of a change. Implemented by <see cref="OmniPreferences"/>.
/// </summary>
internal interface IPreferenceReadsHost
{
    /// <summary>Whether the host registered a store.</summary>
    bool HasStore { get; }

    /// <summary>Keeps <paramref name="work"/> among the background work the service waits for.</summary>
    void Track(Task work);

    /// <summary>Runs store work under the service's lifetime, a failure logged rather than thrown.</summary>
    Task RunInBackgroundAsync(Func<CancellationToken, Task> work);

    /// <summary>Raises a change of a preference.</summary>
    Task RaiseAsync(OmniPreferenceChangedEventArgs change);
}
