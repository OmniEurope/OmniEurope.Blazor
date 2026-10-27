using System.Globalization;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Selection;

/// <summary>The Lists page: the settings of a list box and a check box list, then the lists for other value types.</summary>
public partial class ListsPage
{
    [Inject]
    private IStringLocalizer<SelectionStrings> Text { get; set; } = default!;

    private double VisibleRowsValue { get; set; } = 5;

    private bool ShowLegend { get; set; } = true;

    private bool DisableOption { get; set; } = true;

    private bool ListsDisabled { get; set; }

    private int VisibleRows => (int)VisibleRowsValue;

    private string FormatRows(double rows) =>
        string.Format(CultureInfo.CurrentCulture, Text["ListsRowsValue"], rows.ToString("0", CultureInfo.CurrentCulture));
}
