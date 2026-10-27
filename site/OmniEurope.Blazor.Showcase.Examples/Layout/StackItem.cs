using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>One button of the stack examples: its icon, its label and its size.</summary>
/// <param name="Icon">The icon shown before the label.</param>
/// <param name="Label">The label of the button.</param>
/// <param name="Size">The size of the button; buttons of three sizes make an alignment visible.</param>
public sealed record StackItem(OmniIconName Icon, string Label, OmniControlSize Size)
{
    /// <summary>Eight actions: enough to fill a row and make it overflow.</summary>
    public static IReadOnlyList<StackItem> All { get; } =
    [
        new(OmniIconName.Check, "Validate", OmniControlSize.Small),
        new(OmniIconName.Edit, "Edit", OmniControlSize.Large),
        new(OmniIconName.Copy, "Copy", OmniControlSize.Medium),
        new(OmniIconName.Download, "Download", OmniControlSize.Small),
        new(OmniIconName.Upload, "Upload", OmniControlSize.Large),
        new(OmniIconName.Archive, "Archive", OmniControlSize.Medium),
        new(OmniIconName.Refresh, "Refresh", OmniControlSize.Small),
        new(OmniIconName.Settings, "Settings", OmniControlSize.Medium)
    ];
}
