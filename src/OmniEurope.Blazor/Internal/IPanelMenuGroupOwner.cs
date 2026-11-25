
namespace OmniEurope.Blazor.Components;

/// <summary>
/// What the entries nested in a panel-menu group ask of the group entry itself: whether it, or a group above
/// it, matches the menu's filter, and to look again when an entry under it comes, goes or changes. The group
/// context holds its owner through this contract, so it never names the entry component, which reads it.
/// </summary>
internal interface IPanelMenuGroupOwner
{
    /// <summary>Whether the group entry, or a group above it, matches <paramref name="filter"/>.</summary>
    bool InMatchedBranch(OmniPanelMenuFilter filter);

    /// <summary>Redraws the group entry for the filter and passes the news up to the groups above it.</summary>
    void RefreshForFilter();
}
