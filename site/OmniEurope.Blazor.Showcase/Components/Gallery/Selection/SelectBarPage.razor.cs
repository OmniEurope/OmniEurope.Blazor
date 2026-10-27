using System.Globalization;
using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Selection;

/// <summary>The Select bar page: the number of options, a disabled option and the state of the bars, then the bars for other value types.</summary>
public partial class SelectBarPage
{
    private static readonly int[] OptionCounts = [2, 3, 5];

    [Inject]
    private IStringLocalizer<SelectionStrings> Text { get; set; } = default!;

    private int OptionCount { get; set; } = 5;

    private bool DisableOption { get; set; } = true;

    private bool BarDisabled { get; set; }

    private static IReadOnlyList<OmniOption<int>> CountOptions =>
    [
        .. OptionCounts.Select(count => new OmniOption<int>(count, count.ToString(CultureInfo.CurrentCulture)))
    ];
}
