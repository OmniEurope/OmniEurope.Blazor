using OmniEurope.Blazor.Showcase.Resources;

namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class LayoutDemo
{
    // The key names the card's id after the density, in lower case like data-omni-density: derived
    // from the enum rather than written a second time.
    private static readonly (OmniDensity Density, string Key, string TitleKey)[] Densities =
    [
        (OmniDensity.Compact, KeyOf(OmniDensity.Compact), "DemoLayoutDensityCompact"),
        (OmniDensity.Comfortable, KeyOf(OmniDensity.Comfortable), "DemoLayoutDensityComfortable"),
        (OmniDensity.Spacious, KeyOf(OmniDensity.Spacious), "DemoLayoutDensitySpacious")
    ];

    private static string KeyOf(OmniDensity density) => density.ToString().ToLowerInvariant();

    [Inject] private IStringLocalizer<ShowcaseStrings> Text { get; set; } = default!;

    private bool EmailAlerts { get; set; } = true;

    private bool WeeklyDigest { get; set; }

    private bool OptionsExpanded { get; set; }

    private string? ServerName { get; set; } = "srv-paris-02";

    private string? ServerId { get; set; } = "a9f3cb0";

    private string? ServerZone { get; set; } = "eu-west";

    private bool LockedRestore { get; set; } = true;

    private string? DensityTab { get; set; } = "general";
}
