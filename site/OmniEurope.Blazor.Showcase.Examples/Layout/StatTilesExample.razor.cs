using System.Globalization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

namespace OmniEurope.Blazor.Showcase.Examples.Layout;

/// <summary>A stat tile whose value, title, detail line, icon and click are parameters.</summary>
public partial class StatTilesExample
{
    /// <summary>The figure itself, already formatted: the tile only shows the text it is given.</summary>
    [Parameter]
    public string Value { get; set; } = 42.ToString(CultureInfo.CurrentCulture);

    /// <summary>What the figure measures.</summary>
    [Parameter]
    public string Title { get; set; } = "Destinations";

    /// <summary>Whether a muted detail line sits under the title.</summary>
    [Parameter]
    public bool ShowDetail { get; set; } = true;

    /// <summary>Whether a decorative icon sits on a tinted square before the text.</summary>
    [Parameter]
    public bool ShowIcon { get; set; } = true;

    /// <summary>Whether the whole tile is one button, named by its title and value.</summary>
    [Parameter]
    public bool Clickable { get; set; } = true;

    private int Clicks { get; set; }

    private string? Detail => ShowDetail ? "3 added this week" : null;

    /// <summary>The tile becomes a button only when OnClick has a delegate: switched off, it gets none.</summary>
    private EventCallback<MouseEventArgs> Click =>
        Clickable ? EventCallback.Factory.Create<MouseEventArgs>(this, () => Clicks++) : default;
}
