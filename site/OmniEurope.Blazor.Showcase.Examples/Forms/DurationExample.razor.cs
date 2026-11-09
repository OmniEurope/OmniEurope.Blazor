namespace OmniEurope.Blazor.Showcase.Examples.Forms;

/// <summary>A duration field, a capped one, then the same duration read only and disabled.</summary>
public partial class DurationExample
{
    private TimeSpan? Spent { get; set; } = new TimeSpan(1, 30, 0);

    private TimeSpan? Capped { get; set; }

    private TimeSpan? Frozen { get; set; } = new TimeSpan(2, 15, 0);
}
