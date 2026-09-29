namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class LayoutDemo
{
    // The key names the card's id after the density, in lower case like data-omni-density: derived
    // from the enum rather than written a second time.
    private static readonly (OmniDensity Density, string Key, string Title)[] Densities =
    [
        (OmniDensity.Compact, KeyOf(OmniDensity.Compact), "Compacte"),
        (OmniDensity.Comfortable, KeyOf(OmniDensity.Comfortable), "Confortable"),
        (OmniDensity.Spacious, KeyOf(OmniDensity.Spacious), "Aérée")
    ];

    private static string KeyOf(OmniDensity density) => density.ToString().ToLowerInvariant();


    private bool EmailAlerts { get; set; } = true;

    private bool WeeklyDigest { get; set; }

    private bool OptionsExpanded { get; set; }

    private string? ServerName { get; set; } = "srv-paris-02";

    private string? ServerId { get; set; } = "a9f3cb0";

    private string? ServerZone { get; set; } = "eu-west";

    private bool LockedRestore { get; set; } = true;

    private string? DensityTab { get; set; } = "general";
}
