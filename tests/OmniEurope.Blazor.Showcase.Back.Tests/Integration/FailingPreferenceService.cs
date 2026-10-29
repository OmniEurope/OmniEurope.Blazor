using OmniEurope.Blazor.Showcase.Back.Components.Preferences;
using OmniEurope.Blazor.Showcase.Shared.Components.Preferences;

namespace OmniEurope.Blazor.Showcase.Back.Tests.Integration;

/// <summary>A service that fails on every call with an exception the API does not raise on purpose: a 500.</summary>
internal sealed class FailingPreferenceService : IVisitorPreferenceService
{
    public Task<VisitorPreferenceDto?> GetAsync(string? visitorId, string? key, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Simulated failure.");

    public Task<IReadOnlyList<VisitorPreferenceDto>> GetChangedSinceAsync(string? visitorId, DateTimeOffset? since, CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Simulated failure.");

    public Task<VisitorPreferenceDto> SaveAsync(string? visitorId, string? key, SaveVisitorPreferenceRequest? request, string clientPartition,
        CancellationToken cancellationToken) =>
        throw new InvalidOperationException("Simulated failure.");
}
