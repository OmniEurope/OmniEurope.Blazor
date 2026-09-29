namespace OmniEurope.Blazor.Components;

/// <summary>The track of an <see cref="OmniArcGauge"/> with its bounds written under both ends; its values take these bounds.</summary>
/// <remarks>
/// A chart part: it derives from <see cref="ComponentBase"/>, not <see cref="OmniComponentBase"/>, on
/// purpose. It draws SVG inside its gauge, so it takes no <c>Id</c>, <c>Class</c> or extra attributes.
/// </remarks>
public partial class OmniArcGaugeScale
{
    /// <summary>The value at the left end of the track.</summary>
    [Parameter] public double Minimum { get; set; }

    /// <summary>The value at the right end of the track.</summary>
    [Parameter] public double Maximum { get; set; } = 100;

    /// <summary>The values drawn on the scale, <see cref="OmniArcGaugeScaleValue"/> components.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }
}
