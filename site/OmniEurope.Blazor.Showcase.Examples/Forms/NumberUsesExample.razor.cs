namespace OmniEurope.Blazor.Showcase.Examples.Forms;

/// <summary>Numeric fields in typical uses, with a clamped one and the disabled and read-only states.</summary>
public partial class NumberUsesExample
{
    private int Rating { get; set; } = 7;

    private decimal Price { get; set; } = 12m;

    private int Quantity { get; set; } = 250;

    private double Ratio { get; set; }

    private int Clamped { get; set; } = 4;

    private int FixedDisabled { get; set; } = 42;

    private int FixedReadOnly { get; set; } = 2026;
}
