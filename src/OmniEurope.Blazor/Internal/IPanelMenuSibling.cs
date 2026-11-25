
namespace OmniEurope.Blazor.Components;

/// <summary>
/// What the entries of one panel-menu level ask of each other: how closely each matches the route, and
/// whether it, or an entry nested under it, matches the menu's filter. The level holds its entries through
/// this contract, so it never names the entry component, which reads the level.
/// </summary>
internal interface IPanelMenuSibling
{
    /// <summary>How closely the entry's address matches the route; null when it does not match at all.</summary>
    int? RouteRank { get; }

    /// <summary>Whether the entry, or an entry nested under it, matches <paramref name="filter"/>.</summary>
    bool MatchesFilterWithin(OmniPanelMenuFilter filter);
}
