using System.Globalization;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>A settings section whose description, tile icons, tile descriptions and tile content are parameters.</summary>
public partial class SettingsSectionsExample
{
    private const int DefaultSessionMinutes = 30;

    private static readonly int[] SessionChoices = [15, 30, 60, 240];

    /// <summary>Whether the section carries a line of explanation under its title.</summary>
    [Parameter]
    public bool ShowSectionDescription { get; set; } = true;

    /// <summary>Whether each tile shows its icon.</summary>
    [Parameter]
    public bool ShowIcons { get; set; } = true;

    /// <summary>Whether each tile shows its description under the name.</summary>
    [Parameter]
    public bool ShowDescriptions { get; set; } = true;

    /// <summary>Whether the tiles are drawn flat, with no fill and no frame, a hairline between them.</summary>
    [Parameter]
    public bool Flat { get; set; }

    /// <summary>Where each tile carries its control.</summary>
    [Parameter]
    public SettingsTileContent Content { get; set; } = SettingsTileContent.Both;

    private bool EmailAlerts { get; set; } = true;

    private DigestFrequency Frequency { get; set; } = DigestFrequency.Daily;

    private double TextScale { get; set; } = 100;

    private int SessionMinutes { get; set; } = DefaultSessionMinutes;

    private bool ShowControl => Content is SettingsTileContent.Control or SettingsTileContent.Both;

    private bool ShowDetails => Content is SettingsTileContent.Details or SettingsTileContent.Both;

    private IReadOnlyList<OmniOption<DigestFrequency>> FrequencyOptions { get; } =
    [
        new(DigestFrequency.Immediate, "Immediately"),
        new(DigestFrequency.Daily, "Once a day"),
        new(DigestFrequency.Weekly, "Once a week")
    ];

    private IReadOnlyList<OmniOption<int>> SessionOptions { get; } =
        [.. SessionChoices.Select(minutes => new OmniOption<int>(minutes, $"{minutes} minutes"))];

    private static string FormatPercent(double value) => $"{value.ToString("0", CultureInfo.InvariantCulture)} %";

    private void ResetSession() => SessionMinutes = DefaultSessionMinutes;

    /// <summary>How often the email digest is sent.</summary>
    private enum DigestFrequency
    {
        Immediate,
        Daily,
        Weekly
    }
}
