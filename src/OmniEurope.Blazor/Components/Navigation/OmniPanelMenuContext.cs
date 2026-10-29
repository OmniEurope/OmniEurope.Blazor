namespace OmniEurope.Blazor.Components;

/// <summary>
/// What a panel menu tells the items nested inside it. An item has to know it is down to its icon:
/// with no room for a label and no chevron to click, its own icon is the only thing left that can
/// open the group, so it stops navigating and starts unfolding.
/// </summary>
/// <param name="Display">How items render, after the sidebar has had its say.</param>
internal sealed record OmniPanelMenuContext(OmniPanelMenuDisplay Display)
{
    public bool IconsOnly => Display == OmniPanelMenuDisplay.Icon;

    /// <summary>Opens the sidebar the menu is nested in, when there is one that can be opened.</summary>
    public Func<Task>? ExpandSidebar { get; init; }

    /// <summary>The entries at the first level of the menu, which arbitrate between themselves which one is current.</summary>
    public OmniPanelMenuSiblings Siblings { get; init; } = new();

    /// <summary>The menu's navigation guard, <see cref="OmniPanelMenu.CanNavigate"/>.</summary>
    public Func<string, Task<bool>>? CanNavigate { get; init; }

    /// <summary>The text the menu is narrowed by, <see cref="OmniPanelMenuFilter.None"/> when it is not.</summary>
    public OmniPanelMenuFilter Filter { get; init; } = OmniPanelMenuFilter.None;

    /// <summary>Tells the menu an entry came or went while it is filtered, so its "no match" line follows.</summary>
    public Action? ItemsChanged { get; init; }
}
