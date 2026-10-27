using OmniEurope.Blazor.Showcase.Shared.Components.Preferences;

namespace OmniEurope.Blazor.Showcase.Back.Components.Preferences;

/// <summary>What a write did.</summary>
public enum VisitorPreferenceSaveOutcome
{
    /// <summary>The value is now the stored one.</summary>
    Stored,

    /// <summary>The store already held a value at least as recent, which was kept.</summary>
    KeptNewer,
}

/// <summary>
/// The preferences of anonymous visitors. Every method takes the raw visitor id of the request header
/// and refuses a missing or malformed one with a <see cref="Exceptions.BadRequestException"/>.
/// </summary>
public interface IVisitorPreferenceService
{
    /// <summary>One preference, or null when the visitor never stored it (an unknown visitor holds nothing).</summary>
    Task<VisitorPreferenceDto?> GetAsync(string? visitorId, string? key, CancellationToken cancellationToken);

    /// <summary>The preferences changed at or after <paramref name="since"/>, every one when null.</summary>
    Task<IReadOnlyList<VisitorPreferenceDto>> GetChangedSinceAsync(string? visitorId, DateTimeOffset? since, CancellationToken cancellationToken);

    /// <summary>Stores one preference unless a value at least as recent is stored, within the visitor's quotas.</summary>
    /// <param name="visitorId">The raw visitor id of the request.</param>
    /// <param name="key">The preference key of the route.</param>
    /// <param name="request">The value and its browser date.</param>
    /// <param name="clientPartition">The client address partition, for the visitor creation limit.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<VisitorPreferenceSaveOutcome> SaveAsync(string? visitorId, string? key, SaveVisitorPreferenceRequest? request, string clientPartition,
        CancellationToken cancellationToken);
}
