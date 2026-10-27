using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OmniEurope.Blazor.Showcase.Back.Components.Shared;
using OmniEurope.Blazor.Showcase.Back.Extensions;
using OmniEurope.Blazor.Showcase.Shared.Components.Preferences;

namespace OmniEurope.Blazor.Showcase.Back.Components.Preferences;

/// <summary>
/// The OE preferences of anonymous visitors. Public on purpose: the showcase has no account, the
/// visitor id of the header is the only key to a visitor's rows, and every write is bounded (body size,
/// value size, per-visitor quotas, per-address rates and visitor creations, total visitors).
/// </summary>
[Route(VisitorPreferenceApi.Route)]
[AllowAnonymous]
public sealed class VisitorPreferencesController(IVisitorPreferenceService service) : BaseApiController
{
    /// <summary>The largest request body Kestrel reads for a write, before any parsing.</summary>
    public const long MaxRequestBodyBytes = 128 * 1024;

    [HttpGet]
    [EnableRateLimiting(ShowcaseRateLimiterPolicies.Reads)]
    public async Task<ActionResult<IReadOnlyList<VisitorPreferenceDto>>> GetChanged(
        [FromHeader(Name = VisitorPreferenceApi.VisitorHeader)] string? visitor,
        [FromQuery] DateTimeOffset? since,
        CancellationToken cancellationToken)
    {
        var changed = await service.GetChangedSinceAsync(visitor, since, cancellationToken);
        return Ok(changed);
    }

    /// <summary>The preference, or 204 when the visitor never stored it: a 404 would hide a wrong base address.</summary>
    [HttpGet("{key}")]
    [EnableRateLimiting(ShowcaseRateLimiterPolicies.Reads)]
    public async Task<ActionResult<VisitorPreferenceDto>> Get(
        [FromHeader(Name = VisitorPreferenceApi.VisitorHeader)] string? visitor,
        string key,
        CancellationToken cancellationToken)
    {
        var preference = await service.GetAsync(visitor, key, cancellationToken);
        return preference is null ? NoContent() : Ok(preference);
    }

    [HttpPut("{key}")]
    [EnableRateLimiting(ShowcaseRateLimiterPolicies.Writes)]
    [RequestSizeLimit(MaxRequestBodyBytes)]
    public async Task<IActionResult> Save(
        [FromHeader(Name = VisitorPreferenceApi.VisitorHeader)] string? visitor,
        string key,
        [FromBody] SaveVisitorPreferenceRequest? request,
        CancellationToken cancellationToken)
    {
        await service.SaveAsync(visitor, key, request, ClientPartition, cancellationToken);
        return NoContent();
    }
}
