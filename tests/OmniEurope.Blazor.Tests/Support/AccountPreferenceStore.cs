using System.Collections.Concurrent;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A store per account: the scope is the signed-in <see cref="User"/>, or <see cref="OmniPreferences.NoUserScope"/>
/// when nobody is signed in, and any call made then fails the test. The preference tests derive their own
/// store from it to count, hold or watch the calls.
/// </summary>
internal abstract class AccountPreferenceStore(string? user) : IOmniPreferenceStore
{
    private readonly ConcurrentDictionary<string, MemoryPreferenceStore> _accounts = new(StringComparer.Ordinal);

    private volatile string? _user = user;

    public string? User
    {
        get => _user;
        set => _user = value;
    }

    public MemoryPreferenceStore Of(string user) => _accounts.GetOrAdd(user, name => new MemoryPreferenceStore { Scope = name });

    /// <summary>The store of the signed-in account.</summary>
    protected virtual MemoryPreferenceStore Current() =>
        Of(User ?? throw new InvalidOperationException("No user is signed in: the store must not be called."));

    public virtual Task<OmniPreference?> GetAsync(string key, CancellationToken cancellationToken = default) => Current().GetAsync(key, cancellationToken);

    public Task<IReadOnlyList<OmniPreference>> GetChangedSinceAsync(DateTimeOffset? since, CancellationToken cancellationToken = default) =>
        Current().GetChangedSinceAsync(since, cancellationToken);

    public virtual Task<OmniPreference> SaveAsync(OmniPreference preference, CancellationToken cancellationToken = default) => Current().SaveAsync(preference, cancellationToken);

    public ValueTask<string> GetScopeAsync(CancellationToken cancellationToken = default) => ValueTask.FromResult(User ?? OmniPreferences.NoUserScope);
}
