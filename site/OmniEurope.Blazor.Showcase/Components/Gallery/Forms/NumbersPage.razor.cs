using OmniEurope.Blazor.Showcase.Resources.Gallery;

namespace OmniEurope.Blazor.Showcase.Components.Gallery.Forms;

/// <summary>The Numbers page: the bounds and step of an integer, a decimal and a nullable field, then typical uses.</summary>
public partial class NumbersPage
{
    [Inject]
    private IStringLocalizer<FormsStrings> Text { get; set; } = default!;

    private int? Minimum { get; set; } = 0;

    private int? Maximum { get; set; } = 100;

    private double IntegerStep { get; set; } = 1;

    private double DecimalStep { get; set; } = 0.01;

    private bool ShowPlaceholder { get; set; } = true;

    private bool Disabled { get; set; }

    private bool ReadOnly { get; set; }

    private IReadOnlyList<OmniOption<int?>> MinimumOptions =>
    [
        new(null, Text["NumbersBoundNone"]),
        new(-100, "-100"),
        new(0, "0"),
        new(10, "10")
    ];

    private IReadOnlyList<OmniOption<int?>> MaximumOptions =>
    [
        new(null, Text["NumbersBoundNone"]),
        new(10, "10"),
        new(100, "100"),
        new(1000, "1000")
    ];

    private static IReadOnlyList<OmniOption<double>> IntegerStepOptions { get; } =
    [
        new(1, "1"),
        new(5, "5"),
        new(10, "10"),
        new(100, "100")
    ];

    private static IReadOnlyList<OmniOption<double>> DecimalStepOptions { get; } =
    [
        new(0.01, "0.01"),
        new(0.1, "0.1"),
        new(0.5, "0.5"),
        new(1, "1")
    ];
}
