namespace OmniEurope.Blazor.Components;

/// <summary>
/// The host's durable storage of the user preferences read and written by <see cref="OmniPreferences"/>,
/// usually a table of the host's database. The package ships no implementation: a host registers its
/// own with <c>AddOmniEuropePreferences</c>, and without one the preferences stay in the browser.
/// </summary>
/// <remarks>
/// The implementation is resolved in the user's scope (the circuit under Blazor Server) and answers
/// for the signed-in user only: the package never passes a user identity, the store finds it itself
/// (an <c>AuthenticationStateProvider</c>, a user accessor of the host). The store wins over the
/// browser cache: a value it holds with a later <see cref="OmniPreference.UpdatedAt"/> replaces the
/// cached one. A failure (an exception) is logged by <see cref="OmniPreferences"/> and retried at the
/// next synchronization; the value written stays in the browser meanwhile.
/// <para>
/// <see cref="OmniPreferences"/> never calls the store twice at once: every call (<see cref="GetAsync"/>,
/// <see cref="GetChangedSinceAsync"/>, <see cref="SaveAsync"/>, <see cref="GetScopeAsync"/>) waits for the
/// previous one to complete, including the periodic synchronization that runs off the renderer's thread.
/// A store over a scoped <c>DbContext</c>, which refuses a second operation while one runs, is therefore
/// safe as long as nothing else of the host uses that context at the same time.
/// </para>
/// </remarks>
public interface IOmniPreferenceStore
{
    /// <summary>Reads one preference of the signed-in user.</summary>
    /// <param name="key">The key of the preference, as given to <see cref="OmniPreferences"/>.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The stored preference, a tombstone (<see cref="OmniPreference.Value"/> null) when it was removed, or null when the store never held it.</returns>
    Task<OmniPreference?> GetAsync(string key, CancellationToken cancellationToken = default);

    /// <summary>Reads every preference of the signed-in user changed at or after <paramref name="since"/>, tombstones included.</summary>
    /// <param name="since">The <see cref="OmniPreference.UpdatedAt"/> from which to read; null reads every preference.</param>
    /// <param name="cancellationToken">Cancels the read.</param>
    /// <returns>The preferences changed since that moment, in any order.</returns>
    Task<IReadOnlyList<OmniPreference>> GetChangedSinceAsync(DateTimeOffset? since, CancellationToken cancellationToken = default);

    /// <summary>
    /// Writes one preference of the signed-in user, replacing the stored one with the same key. A
    /// preference whose <see cref="OmniPreference.Value"/> is null is a tombstone: keep it, with its
    /// date, so the removal reaches the user's other devices.
    /// </summary>
    /// <param name="preference">The preference to store, with the date of the change.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    /// <returns>A task that completes once the preference is stored.</returns>
    Task SaveAsync(OmniPreference preference, CancellationToken cancellationToken = default);

    /// <summary>
    /// The partition of the browser cache that belongs to the signed-in user, so two users of the same
    /// browser never read each other's cached preferences. It is written in clear in the browser storage
    /// keys: it must not be personal data (an e-mail address, a name, a raw user id) but an opaque value,
    /// a hash of the user id for instance. It must not contain a colon, and the same user must always get
    /// the same value.
    /// </summary>
    /// <param name="cancellationToken">Cancels the lookup.</param>
    /// <returns>The opaque, non-personal scope of the signed-in user.</returns>
    ValueTask<string> GetScopeAsync(CancellationToken cancellationToken = default);
}
