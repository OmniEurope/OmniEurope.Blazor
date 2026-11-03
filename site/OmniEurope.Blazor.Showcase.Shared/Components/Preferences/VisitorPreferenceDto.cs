namespace OmniEurope.Blazor.Showcase.Shared.Components.Preferences;

/// <summary>One preference of an anonymous visitor, as the API returns it.</summary>
/// <param name="Key">The key given to <c>OmniPreferences</c> (<c>omni.appearance</c>).</param>
/// <param name="Value">The value serialized as JSON; null is a tombstone (the preference was removed).</param>
/// <param name="UpdatedAt">When the browser wrote the value; the later of two values wins.</param>
/// <param name="ReceivedAt">
/// When the server recorded the value, on its own clock: the cursor of <c>GET api/preferences?since=</c>, never compared to
/// arbitrate. Optional so a browser still reads the answer of an API deployed before it (the cursor then stays on the date).
/// </param>
public sealed record VisitorPreferenceDto(string Key, string? Value, DateTimeOffset UpdatedAt, DateTimeOffset? ReceivedAt = null);
