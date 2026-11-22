using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Tests;

/// <summary>
/// A store that hands every call to <paramref name="inner"/>: a test store derives from it and overrides
/// only the call whose behaviour it changes.
/// </summary>
internal abstract class DelegatingPreferenceStore(IOmniPreferenceStore inner) : IOmniPreferenceStore
{
    protected IOmniPreferenceStore Inner { get; } = inner;

    public virtual Task<OmniPreference?> GetAsync(string key, CancellationToken cancellationToken = default) =>
        Inner.GetAsync(key, cancellationToken);

    public virtual Task<IReadOnlyList<OmniPreference>> GetChangedSinceAsync(DateTimeOffset? since, CancellationToken cancellationToken = default) =>
        Inner.GetChangedSinceAsync(since, cancellationToken);

    public virtual Task<OmniPreference> SaveAsync(OmniPreference preference, CancellationToken cancellationToken = default) =>
        Inner.SaveAsync(preference, cancellationToken);

    public virtual ValueTask<string> GetScopeAsync(CancellationToken cancellationToken = default) =>
        Inner.GetScopeAsync(cancellationToken);
}
