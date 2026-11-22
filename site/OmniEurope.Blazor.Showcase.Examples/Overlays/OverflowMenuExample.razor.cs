using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Overlays;

/// <summary>A list of files, each row ending with a three-dot menu whose trigger and entries are parameters.</summary>
public partial class OverflowMenuExample
{
    /// <summary>The size of the three-dot trigger.</summary>
    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Medium;

    /// <summary>Whether the triggers are inactive: the menus do not open.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    /// <summary>Whether a destructive Delete entry ends each menu.</summary>
    [Parameter]
    public bool ShowDelete { get; set; } = true;

    /// <summary>Whether the Rename entry is disabled: it stays listed, dimmed, and the arrows skip it.</summary>
    [Parameter]
    public bool DisableRename { get; set; }

    /// <summary>The file whose menu is open: one menu of the list at a time.</summary>
    private SingleOpenMenu Menus { get; } = new();

    private string? LastAction { get; set; }

    private void Choose(string file, string action) => LastAction = $"{action}: {file}";
}
