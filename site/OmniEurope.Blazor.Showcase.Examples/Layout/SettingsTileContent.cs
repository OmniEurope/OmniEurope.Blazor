namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>Where a settings tile of the preview carries its control.</summary>
public enum SettingsTileContent
{
    /// <summary>Beside the name (ChildContent).</summary>
    Control,

    /// <summary>Under the row (Details).</summary>
    Details,

    /// <summary>Both beside the name and under the row.</summary>
    Both,

    /// <summary>Nowhere: the tile shows its name only.</summary>
    None
}
