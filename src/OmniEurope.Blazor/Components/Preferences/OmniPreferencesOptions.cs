namespace OmniEurope.Blazor.Components;

/// <summary>How <see cref="OmniPreferences"/> keeps the browser cache in step with the host's <see cref="IOmniPreferenceStore"/>.</summary>
public sealed class OmniPreferencesOptions
{
    /// <summary>
    /// The interval of the periodic synchronization; one minute by default. <see cref="Timeout.InfiniteTimeSpan"/>
    /// turns the periodic synchronization off: the cache then follows the store when a tab becomes
    /// visible again, when a preference is first read and on <see cref="OmniPreferences.SyncAsync"/>.
    /// Any other value must be positive.
    /// </summary>
    public TimeSpan SyncInterval { get; set; } = TimeSpan.FromMinutes(1);
}
