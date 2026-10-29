using OmniEurope.Blazor.Showcase.Shared.Components.Preferences;

namespace OmniEurope.Blazor.Showcase.Back.Components.Preferences;

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
    /// <returns>
    /// The preference the server now holds for the key, with the date it keeps (the server is the authority
    /// for dates): the written value at its date, or at the server's time when that date was in the future,
    /// or the later value it already held.
    /// </returns>
    /// <param name="visitorId">The raw visitor id of the request.</param>
    /// <param name="key">The preference key of the route.</param>
    /// <param name="request">The value and its browser date.</param>
    /// <param name="clientPartition">The client address partition, for the visitor creation limit.</param>
    /// <param name="cancellationToken">Cancels the write.</param>
    Task<VisitorPreferenceDto> SaveAsync(string? visitorId, string? key, SaveVisitorPreferenceRequest? request, string clientPartition,
        CancellationToken cancellationToken);
}
