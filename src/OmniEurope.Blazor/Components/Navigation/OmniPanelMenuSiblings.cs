namespace OmniEurope.Blazor.Components;

/// <summary>
/// The entries sitting at one level of a panel menu, the menu's own or a group's. They are what lets an
/// entry know a sibling matches the route more closely: two siblings whose addresses prefix one another
/// (<c>/settings</c> and <c>/settings/users</c>) both match the deeper page, and only the deeper one
/// is the page the reader is on.
/// </summary>
internal sealed class OmniPanelMenuSiblings
{
    private readonly List<OmniPanelMenuItem> _items = [];

    public void Add(OmniPanelMenuItem item) => _items.Add(item);

    public void Remove(OmniPanelMenuItem item) => _items.Remove(item);

    /// <summary>
    /// True when another entry of this level matches the route with a longer address. An exact match is
    /// always the longest address that can match, so it needs no rule of its own.
    /// </summary>
    public bool IsOutranked(OmniPanelMenuItem item, int rank) =>
        _items.Any(other => !ReferenceEquals(other, item) && other.RouteRank > rank);
}
