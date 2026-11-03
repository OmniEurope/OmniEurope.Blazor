namespace OmniEurope.Blazor.Components;

/// <summary>One user preference as an <see cref="IOmniPreferenceStore"/> keeps it.</summary>
/// <param name="Key">The key of the preference, as given to <see cref="OmniPreferences"/>.</param>
/// <param name="Value">The value, serialized as JSON; null is a tombstone: the preference was removed or reset, and that removal is synchronized like any change.</param>
/// <param name="UpdatedAt">When the value was written, as dated by the device that wrote it (or as the store corrected it); the later of two values wins. It is the only date compared to arbitrate between two values.</param>
/// <param name="ReceivedAt">
/// When the store recorded this value, on the store's own clock; null when the store does not track it.
/// It is never used to arbitrate between two values, only as the synchronization cursor: the next
/// <see cref="IOmniPreferenceStore.GetChangedSinceAsync"/> asks for what the store received at or after the
/// latest one already read. A store that sets it must assign it so that a value which becomes readable later
/// never carries an earlier <c>ReceivedAt</c> than a value of the same user already readable (serialize the
/// writes of a user and take the later of the store's clock and the user's last <c>ReceivedAt</c> plus one
/// tick, or equivalent). A store that leaves it null keeps the cursor on <see cref="UpdatedAt"/>, with its
/// limit: a value written later but dated before the cursor (another device, a delayed write) is never read
/// incrementally, and only a full read (a new browser cache) brings it.
/// </param>
public sealed record OmniPreference(string Key, string? Value, DateTimeOffset UpdatedAt, DateTimeOffset? ReceivedAt = null);
