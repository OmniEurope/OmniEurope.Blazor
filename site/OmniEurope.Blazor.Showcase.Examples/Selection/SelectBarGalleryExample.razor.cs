using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Selection;

/// <summary>Select bars bound to an enumeration, an integer and a boolean, and a disabled one.</summary>
public partial class SelectBarGalleryExample
{
    private static readonly int[] ZoomLevels = [50, 75, 100, 150, 200];

    private OmniDensity Density { get; set; } = OmniDensity.Comfortable;

    private int Zoom { get; set; } = 100;

    private bool Confirmed { get; set; } = true;

    private string FrozenPeriod { get; set; } = "week";

    private static IReadOnlyList<OmniOption<OmniDensity>> DensityOptions =>
    [
        new(OmniDensity.Compact, "Compact"),
        new(OmniDensity.Comfortable, "Comfortable"),
        new(OmniDensity.Spacious, "Spacious")
    ];

    private static IReadOnlyList<OmniOption<int>> ZoomOptions =>
    [
        .. ZoomLevels.Select(level => new OmniOption<int>(level, $"{level} %"))
    ];

    private static IReadOnlyList<OmniOption<bool>> YesNoOptions =>
    [
        new(true, "Yes"),
        new(false, "No")
    ];

    private static IReadOnlyList<OmniOption<string>> PeriodOptions =>
    [
        new("day", "Day"),
        new("week", "Week"),
        new("month", "Month")
    ];
}
