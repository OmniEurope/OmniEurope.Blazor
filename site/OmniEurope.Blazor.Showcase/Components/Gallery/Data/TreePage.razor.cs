using System.Globalization;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Data;

/// <summary>The Tree page: hand-written and generated trees, custom item content, moving items and a tree grid, with the selection mode, opening, disabled items and branch loading as settings.</summary>
public partial class TreePage
{
    [Inject]
    private IStringLocalizer<DataStrings> Text { get; set; } = default!;

    private bool Multiple { get; set; }

    private bool StartExpanded { get; set; } = true;

    private bool DisableItems { get; set; } = true;

    private double LatencyValue { get; set; } = 800;

    private bool FailLoad { get; set; }

    private string FormatMilliseconds(double milliseconds) =>
        string.Format(CultureInfo.CurrentCulture, Text["TreeMilliseconds"], milliseconds.ToString("0", CultureInfo.CurrentCulture));
}
