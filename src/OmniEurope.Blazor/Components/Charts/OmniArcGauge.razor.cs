namespace OmniEurope.Blazor.Components;

/// <summary>
/// A half-circle gauge: an <see cref="OmniArcGaugeScale"/> with its bounds, and one or more
/// <see cref="OmniArcGaugeScaleValue"/> drawn on it.
/// </summary>
/// <remarks>
/// The drawing is one image for assistive technology, named by <see cref="Label"/> followed by the
/// values as they are written on the gauge ("Progress: 75"), so the value is heard even when
/// <see cref="OmniArcGaugeScaleValue.ShowValue"/> hides it from sight.
/// </remarks>
public partial class OmniArcGauge
{
    private readonly List<(object Owner, string Text)> _values = [];

    /// <summary>What the gauge measures; null (the default) takes the localized "Gauge".</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>The scale and its values.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    private string EffectiveLabel => string.IsNullOrWhiteSpace(Label) ? Localize("ArcGaugeLabel") : Label;

    /// <summary>The accessible name: the label, then the values written on the gauge, in the order they registered.</summary>
    internal string AccessibleName => _values.Count == 0
        ? EffectiveLabel
        : Localize("LabelValuePair", EffectiveLabel, string.Join(", ", _values.Select(value => value.Text)));

    /// <summary>Records the text a value writes, and names the gauge again when it changed.</summary>
    internal void SetValue(object owner, string text)
    {
        var index = _values.FindIndex(value => ReferenceEquals(value.Owner, owner));
        if (index >= 0 && _values[index].Text == text)
        {
            return;
        }

        if (index >= 0)
        {
            _values[index] = (owner, text);
        }
        else
        {
            _values.Add((owner, text));
        }

        _ = InvokeAsync(StateHasChanged);
    }

    /// <summary>Forgets a value that left the gauge.</summary>
    internal void RemoveValue(object owner)
    {
        if (_values.RemoveAll(value => ReferenceEquals(value.Owner, owner)) > 0)
        {
            _ = InvokeAsync(StateHasChanged);
        }
    }
}
