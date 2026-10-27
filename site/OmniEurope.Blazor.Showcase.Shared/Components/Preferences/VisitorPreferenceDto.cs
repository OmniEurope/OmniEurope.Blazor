namespace OmniEurope.Blazor.Showcase.Shared.Components.Preferences;

/// <summary>One preference of an anonymous visitor, as the API returns it.</summary>
/// <param name="Key">The key given to <c>OmniPreferences</c> (<c>omni.appearance</c>).</param>
/// <param name="Value">The value serialized as JSON; null is a tombstone (the preference was removed).</param>
/// <param name="UpdatedAt">When the browser wrote the value; the later of two values wins.</param>
public sealed record VisitorPreferenceDto(string Key, string? Value, DateTimeOffset UpdatedAt);
