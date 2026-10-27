using System.ComponentModel.DataAnnotations;

namespace OmniEurope.Blazor.Showcase.Back.Components.Preferences;

/// <summary>
/// How long an idle visitor is kept (section <c>Preferences:Retention</c>, environment variables
/// <c>Preferences__Retention__*</c>). Validated on start.
/// </summary>
public sealed class VisitorRetentionOptions
{
    public const string Section = "Preferences:Retention";

    /// <summary>A visitor neither read nor written for this many days is deleted with its preferences.</summary>
    [Range(1, 3650)]
    public int IdleDays { get; set; } = 60;

    /// <summary>The time between two sweeps; the first one runs at start-up.</summary>
    [Range(typeof(TimeSpan), "00:01:00", "7.00:00:00")]
    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromHours(6);

    /// <summary>The most visitors one delete statement removes; a sweep repeats it until none is left.</summary>
    [Range(1, 100_000)]
    public int BatchSize { get; set; } = 1_000;
}
