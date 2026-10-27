using System.Globalization;
using Microsoft.AspNetCore.Components;
using OmniEurope.Blazor.Components;

namespace OmniEurope.Blazor.Showcase.Examples.Feedback;

/// <summary>A progress gauge whose shape, tone, value and texts are parameters.</summary>
public partial class ProgressExample
{
    /// <summary>A bar or a ring.</summary>
    [Parameter]
    public OmniProgressShape Shape { get; set; } = OmniProgressShape.Linear;

    /// <summary>The colour says what the progress means: normal, done, at risk or failing.</summary>
    [Parameter]
    public OmniTone Tone { get; set; } = OmniTone.Accent;

    /// <summary>The value, from 0 to 100; ignored while the gauge is indeterminate.</summary>
    [Parameter]
    public double Value { get; set; } = 40;

    /// <summary>Whether the work has no known end: the gauge moves without a value.</summary>
    [Parameter]
    public bool Indeterminate { get; set; }

    /// <summary>Whether the percentage, or the custom text, is written next to the gauge.</summary>
    [Parameter]
    public bool ShowValue { get; set; } = true;

    /// <summary>Whether a custom text replaces the percentage, on screen and for screen readers.</summary>
    [Parameter]
    public bool CustomValueText { get; set; }

    /// <summary>The accessible name; left empty, the component uses its own default name.</summary>
    [Parameter]
    public string Label { get; set; } = string.Empty;

    private string? ValueText => CustomValueText
        ? string.Create(CultureInfo.CurrentCulture, $"{Math.Round(Value)} files processed out of 100")
        : null;

    private string Echo => Indeterminate
        ? "No value: the gauge is indeterminate."
        : string.Create(CultureInfo.CurrentCulture, $"Current value: {Math.Round(Value)} %.");
}
