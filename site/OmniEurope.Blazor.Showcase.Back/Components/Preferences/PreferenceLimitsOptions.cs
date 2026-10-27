using System.ComponentModel.DataAnnotations;

namespace OmniEurope.Blazor.Showcase.Back.Components.Preferences;

/// <summary>
/// The quotas of the anonymous write surface (section <c>Preferences:Limits</c>, environment variables
/// <c>Preferences__Limits__*</c>). Validated on start: an out-of-range value stops the API.
/// </summary>
public sealed class PreferenceLimitsOptions
{
    public const string Section = "Preferences:Limits";

    /// <summary>The longest JSON value of one preference, in characters.</summary>
    [Range(256, 1_048_576)]
    public int MaxValueLength { get; set; } = 16_384;

    /// <summary>The most keys a visitor holds, tombstones included.</summary>
    [Range(1, 10_000)]
    public int MaxKeysPerVisitor { get; set; } = 100;

    /// <summary>The most characters of JSON a visitor holds over all its keys.</summary>
    [Range(256, 16_777_216)]
    public int MaxStoredLengthPerVisitor { get; set; } = 65_536;

    /// <summary>The most visitors the database holds; past it, a new visitor is refused (503) and stays in its browser.</summary>
    [Range(1, 100_000_000)]
    public int MaxVisitors { get; set; } = 50_000;

    /// <summary>The most new visitors one client address (an IPv4 address or an IPv6 /64) creates per hour.</summary>
    [Range(1, 100_000)]
    public int NewVisitorsPerAddressPerHour { get; set; } = 20;
}
