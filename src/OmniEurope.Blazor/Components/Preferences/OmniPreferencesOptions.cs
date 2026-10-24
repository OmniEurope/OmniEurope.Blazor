namespace OmniEurope.Blazor.Components;

/// <summary>How <see cref="OmniPreferences"/> keeps the browser cache in step with the host's <see cref="IOmniPreferenceStore"/>.</summary>
public sealed class OmniPreferencesOptions
{
    /// <summary>
    /// The interval of the periodic synchronization; one minute by default. <see cref="Timeout.InfiniteTimeSpan"/>
    /// turns the periodic synchronization off: the cache then follows the store when a tab becomes
    /// visible again, when a preference is first read (both limited by <see cref="MinSyncGap"/>) and on
    /// <see cref="OmniPreferences.SyncAsync"/>. A host that learns of a change by its own signal (a SignalR
    /// hub, for instance) can turn the periodic synchronization off and call
    /// <see cref="OmniPreferences.SyncAsync"/> when the signal arrives. Any other value must be positive.
    /// </summary>
    public TimeSpan SyncInterval { get; set; } = TimeSpan.FromMinutes(1);

    /// <summary>
    /// The shortest time between two automatic synchronizations that no timer and no caller asked for: a
    /// tab becoming visible again, or a preference first read, does not synchronize when the cache was
    /// synchronized less than this ago (the date is kept in the browser cache, so it holds across page loads
    /// and tabs of the same browser and user). Thirty seconds by default. The periodic synchronization
    /// (<see cref="SyncInterval"/>) and <see cref="OmniPreferences.SyncAsync"/> are never limited.
    /// <see cref="TimeSpan.Zero"/> removes the limit; any other value must be positive.
    /// </summary>
    public TimeSpan MinSyncGap { get; set; } = TimeSpan.FromSeconds(30);
}
