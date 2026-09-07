namespace OmniEurope.Blazor.Components;

/// <summary>
/// Which half of the tabs an item is rendering. The strip and the panels are two separate boxes,
/// because only the strip scrolls, so the child content is rendered once for each.
/// </summary>
internal enum OmniTabsPhase
{
    Tab,
    Panel
}
