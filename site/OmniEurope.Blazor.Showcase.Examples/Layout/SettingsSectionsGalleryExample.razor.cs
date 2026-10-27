namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>A privacy section: tiles with a switch, a check box, a three-state switch and actions.</summary>
public partial class SettingsSectionsGalleryExample
{
    private bool PublicProfile { get; set; }

    private bool KeepHistory { get; set; } = true;

    private bool Newsletter { get; set; }

    private bool? SyncMode { get; set; }

    private string? Last { get; set; }

    private static string OnOff(bool value) => value ? "on" : "off";
}
