using System.ComponentModel.DataAnnotations;

namespace OmniEurope.Blazor.Showcase.Back.Extensions;

/// <summary>The names of the rate limiting policies, referenced by the controllers.</summary>
public static class ShowcaseRateLimiterPolicies
{
    public const string Reads = "preferences-read";

    public const string Writes = "preferences-write";
}

/// <summary>
/// The per-address request rates (section <c>RateLimiting</c>, environment variables
/// <c>RateLimiting__*</c>). A page load reads each preference once and the synchronization reads the
/// changes once a minute, so the defaults leave a visitor plenty of room. Validated on start.
/// </summary>
public sealed class RateLimitingOptions
{
    public const string Section = "RateLimiting";

    [Range(1, 100_000)]
    public int ReadsPerMinute { get; set; } = 240;

    [Range(1, 100_000)]
    public int WritesPerMinute { get; set; } = 60;
}
