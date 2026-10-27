using System.Globalization;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Foundations;

/// <summary>A filterable gallery of every icon of the set, one button each.</summary>
public partial class IconGalleryExample
{
    private static readonly IReadOnlyList<OmniIconName> AllIcons = IconCatalog.All;

    /// <summary>The size of the icons in the gallery.</summary>
    [Parameter]
    public OmniControlSize Size { get; set; } = OmniControlSize.Large;

    /// <summary>The icon picked, shown pressed.</summary>
    [Parameter]
    public OmniIconName Selected { get; set; } = OmniIconName.Star;

    /// <summary>Raised with the icon picked.</summary>
    [Parameter]
    public EventCallback<OmniIconName> SelectedChanged { get; set; }

    private string Filter { get; set; } = string.Empty;

    private IReadOnlyList<OmniIconName> Filtered
    {
        get
        {
            var query = (Filter ?? string.Empty).Trim();
            return query.Length == 0
                ? AllIcons
                : AllIcons.Where(name => name.ToString().Contains(query, StringComparison.OrdinalIgnoreCase)).ToArray();
        }
    }

    private string Count => string.Format(CultureInfo.CurrentCulture, "{0} of {1} icons", Filtered.Count, AllIcons.Count);

    private Task PickAsync(OmniIconName icon)
    {
        // Shown pressed at once, bound or not; a host that binds Selected hands the same value back.
        Selected = icon;
        return SelectedChanged.InvokeAsync(icon);
    }
}
