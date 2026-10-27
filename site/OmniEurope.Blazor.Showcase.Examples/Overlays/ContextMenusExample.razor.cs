using Microsoft.AspNetCore.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Overlays;

/// <summary>A context menu on a zone, whose entries and behaviour are parameters.</summary>
public partial class ContextMenusExample
{
    /// <summary>Whether a destructive Delete entry ends the menu.</summary>
    [Parameter]
    public bool ShowDelete { get; set; } = true;

    /// <summary>Whether the Rename entry is disabled: it stays visible and the keyboard skips it.</summary>
    [Parameter]
    public bool DisableRename { get; set; }

    /// <summary>Whether the menu stays open once an entry is chosen.</summary>
    [Parameter]
    public bool KeepOpen { get; set; }

    /// <summary>Whether the menu is off: a right click gives the browser's own menu back.</summary>
    [Parameter]
    public bool Disabled { get; set; }

    private bool MenuOpen { get; set; }

    private string? LastAction { get; set; }

    private void Choose(string action)
    {
        LastAction = action;
        MenuOpen = KeepOpen;
    }
}
