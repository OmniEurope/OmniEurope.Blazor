namespace OmniEurope.Blazor.Showcase.Components.Demos;

public partial class LayoutDemo
{
    private static readonly (OmniDensity Density, string Key, string Title)[] Densities =
    [
        (OmniDensity.Compact, "compact", "Compacte"),
        (OmniDensity.Comfortable, "comfortable", "Confortable"),
        (OmniDensity.Spacious, "spacious", "Aérée")
    ];

    private static readonly string[] DensityTabKeys = ["general", "securite"];

    private bool EmailAlerts { get; set; } = true;

    private bool WeeklyDigest { get; set; }

    private bool OptionsCollapsed { get; set; } = true;

    private string? ServerName { get; set; } = "srv-paris-02";

    private string? ServerId { get; set; } = "a9f3cb0";

    private string? ServerZone { get; set; } = "eu-west";

    private bool LockedRestore { get; set; } = true;

    private string? DensityTab { get; set; } = "general";
}
