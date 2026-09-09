namespace OmniEurope.Blazor.Components;

/// <summary>
/// What a panel menu tells the items nested inside it. An item has to know it is down to its icon:
/// with no room for a label and no chevron to click, its own icon is the only thing left that can
/// open the group, so it stops navigating and starts unfolding.
/// </summary>
/// <param name="DisplayStyle">How items render, after the sidebar has had its say.</param>
internal sealed record OmniPanelMenuContext(OmniPanelMenuDisplayStyle DisplayStyle)
{
    public bool IconsOnly => DisplayStyle == OmniPanelMenuDisplayStyle.Icon;
}
