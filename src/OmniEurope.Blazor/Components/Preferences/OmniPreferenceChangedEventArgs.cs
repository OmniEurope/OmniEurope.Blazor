namespace OmniEurope.Blazor.Components;

/// <summary>A preference whose value changed, as <see cref="OmniPreferences.Changed"/> reports it.</summary>
/// <param name="Key">The key of the preference.</param>
/// <param name="Value">The new value, serialized as JSON; null when the preference was removed.</param>
/// <param name="Origin">Where the change comes from.</param>
public sealed record OmniPreferenceChangedEventArgs(string Key, string? Value, OmniPreferenceChangeOrigin Origin);
