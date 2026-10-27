namespace OmniEurope.Blazor.Showcase.Shared.Components.Preferences;

/// <summary>The body of <c>PUT api/preferences/{key}</c>.</summary>
/// <param name="Value">The value serialized as JSON, or null for a tombstone.</param>
/// <param name="UpdatedAt">When the browser wrote the value; a stored value at least as recent is kept.</param>
public sealed record SaveVisitorPreferenceRequest(string? Value, DateTimeOffset UpdatedAt);
