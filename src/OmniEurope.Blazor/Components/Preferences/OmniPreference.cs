namespace OmniEurope.Blazor.Components;

/// <summary>One user preference as an <see cref="IOmniPreferenceStore"/> keeps it.</summary>
/// <param name="Key">The key of the preference, as given to <see cref="OmniPreferences"/>.</param>
/// <param name="Value">The value, serialized as JSON; null is a tombstone: the preference was removed or reset, and that removal is synchronized like any change.</param>
/// <param name="UpdatedAt">When the value was written; the later of two values wins.</param>
public sealed record OmniPreference(string Key, string? Value, DateTimeOffset UpdatedAt);
