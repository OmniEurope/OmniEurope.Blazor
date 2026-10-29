using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Navigation;

/// <summary>The site search in an app bar, over a few sample pages.</summary>
public partial class SiteSearchExample : IDisposable
{
    private static readonly IReadOnlyList<OmniSiteSearchDestination> Pages =
    [
        new("Dashboard", "#dashboard", "General", Icon: OmniIconName.Home),
        new("Orders", "#orders", "Sales", ["invoice", "delivery"], OmniIconName.Layers),
        new("Customers", "#customers", "Sales", ["clients", "contact"], OmniIconName.User),
        new("Invoices", "#invoices", "Sales", ["facture", "billing"], OmniIconName.BookOpen),
        new("Products", "#products", "Catalogue", ["stock", "article"], OmniIconName.Flask),
        new("Settings", "#settings", "General", ["préférences", "options"], OmniIconName.Settings)
    ];

    /// <summary>Whether the pages are listed under their group (Sales, Catalogue...).</summary>
    [Parameter]
    public bool Grouped { get; set; } = true;

    /// <summary>Whether an icon is drawn before each page.</summary>
    [Parameter]
    public bool ShowIcons { get; set; } = true;

    [Inject]
    private NavigationManager Navigation { get; set; } = default!;

    private string? Reached { get; set; }

    /// <summary>The pages given to the search: with or without their group and their icon.</summary>
    private IReadOnlyList<OmniSiteSearchDestination> Destinations =>
    [
        .. Pages.Select(page => page with
        {
            Group = Grouped ? page.Group : null,
            Icon = ShowIcons ? page.Icon : null
        })
    ];

    /// <inheritdoc />
    protected override void OnInitialized() => Navigation.LocationChanged += OnLocationChanged;

    /// <inheritdoc />
    public void Dispose() => Navigation.LocationChanged -= OnLocationChanged;

    private void OnLocationChanged(object? sender, LocationChangedEventArgs args)
    {
        var fragment = new Uri(args.Location).Fragment;
        Reached = string.IsNullOrEmpty(fragment) ? null : fragment;
        InvokeAsync(StateHasChanged);
    }
}
